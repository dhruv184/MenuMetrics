using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MenuMetrics.Web.Models.Entities;

namespace MenuMetrics.Web.Data;

public static class SeedData
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var r in new[] { "Admin", "Owner", "Manager" })
            if (!await roles.RoleExistsAsync(r)) await roles.CreateAsync(new IdentityRole(r));

        if (!db.Businesses.Any())
        {
            var biz = new Business { Name = "Bella's Pizza", Plan = "Starter" };
            biz.Locations.Add(new Location { Name = "Downtown", Address = "123 Main St" });
            var items = new[] {
                new MenuItem { Name = "Margherita Pizza", Category = "Pizza", Price = 14.00m, Cost = 4.20m },
                new MenuItem { Name = "Pepperoni Pizza",  Category = "Pizza", Price = 16.00m, Cost = 5.10m },
                new MenuItem { Name = "Caesar Salad",     Category = "Salad", Price = 9.00m,  Cost = 2.10m },
                new MenuItem { Name = "Garlic Knots",     Category = "Sides", Price = 6.00m,  Cost = 1.10m },
                new MenuItem { Name = "Tiramisu",         Category = "Dessert", Price = 7.50m, Cost = 2.30m },
                new MenuItem { Name = "House Lasagna",    Category = "Pasta", Price = 17.00m, Cost = 6.80m },
            };
            foreach (var m in items) biz.MenuItems.Add(m);
            db.Businesses.Add(biz);
            await db.SaveChangesAsync();

            // Generate 60 days of transactions
            var rng = new Random(7);
            var loc = biz.Locations.First();
            var menu = biz.MenuItems.ToList();
            var txs = new List<Transaction>();
            for (int d = 60; d >= 1; d--)
            {
                var day = DateTime.UtcNow.Date.AddDays(-d);
                int dayOfWeek = (int)day.DayOfWeek;
                double dayFactor = dayOfWeek is 5 or 6 ? 1.6 : dayOfWeek is 0 ? 1.2 : dayOfWeek is 2 ? 0.7 : 1.0;
                for (int h = 10; h < 22; h++)
                {
                    double hourFactor = (h >= 11 && h <= 14) ? 1.4 : (h >= 17 && h <= 21) ? 1.7 : 0.4;
                    int orders = (int)Math.Max(0, rng.NextDouble() * 20 * dayFactor * hourFactor);
                    for (int o = 0; o < orders; o++)
                    {
                        var item = menu[rng.Next(menu.Count)];
                        int qty = rng.Next(1, 3);
                        txs.Add(new Transaction {
                            BusinessId = biz.Id, LocationId = loc.Id, MenuItemId = item.Id,
                            OccurredAt = day.AddHours(h).AddMinutes(rng.Next(60)),
                            Quantity = qty, LineTotal = item.Price * qty, Source = "seed"
                        });
                    }
                }
            }
            db.Transactions.AddRange(txs);

            db.Promotions.AddRange(new[] {
                new Promotion { BusinessId = biz.Id, Name = "Family Tuesday", StartDate = DateTime.UtcNow.AddDays(-40), EndDate = DateTime.UtcNow.AddDays(-15), DiscountPct = 20, PreRevenue = 14200, DuringRevenue = 21800, PostRevenue = 16400, Roi = 2.4m, Rating = "Excellent" },
                new Promotion { BusinessId = biz.Id, Name = "BOGO Pepperoni", StartDate = DateTime.UtcNow.AddDays(-30), EndDate = DateTime.UtcNow.AddDays(-22), DiscountPct = 50, PreRevenue = 18900, DuringRevenue = 22100, PostRevenue = 17200, Roi = 1.3m, Rating = "Good" },
                new Promotion { BusinessId = biz.Id, Name = "Lunch Combo $9.99", StartDate = DateTime.UtcNow.AddDays(-20), EndDate = DateTime.UtcNow.AddDays(-1), DiscountPct = 15, PreRevenue = 16700, DuringRevenue = 17900, PostRevenue = 16200, Roi = 0.7m, Rating = "Neutral" },
            });

            db.InventoryItems.AddRange(new[] {
                new InventoryItem { BusinessId = biz.Id, Sku = "FLR-001", Name = "Flour 50lb", OnHand = 6, ReorderPoint = 4, Unit = "bag" },
                new InventoryItem { BusinessId = biz.Id, Sku = "CHE-002", Name = "Mozzarella", OnHand = 18, ReorderPoint = 12, Unit = "lb" },
                new InventoryItem { BusinessId = biz.Id, Sku = "SAU-003", Name = "Tomato Sauce", OnHand = 9, ReorderPoint = 10, Unit = "gal" },
                new InventoryItem { BusinessId = biz.Id, Sku = "PEP-004", Name = "Pepperoni",   OnHand = 3, ReorderPoint = 5, Unit = "lb" },
            });
            await db.SaveChangesAsync();

            // Demo user
            var u = new ApplicationUser { UserName = "demo@menumetrics.app", Email = "demo@menumetrics.app", EmailConfirmed = true, DisplayName = "Demo Owner", BusinessId = biz.Id };
            var result = await users.CreateAsync(u, "Demo!12345");
            if (result.Succeeded) await users.AddToRoleAsync(u, "Owner");
        }
    }
}
