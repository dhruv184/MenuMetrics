using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using MenuMetrics.Web.Data;
using MenuMetrics.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace MenuMetrics.Web.Services;
public class EtlService(ApplicationDbContext db) : IEtlService
{
    private static readonly Dictionary<string, string[]> SourceSignatures = new()
    {
        ["square"] = new[] { "Transaction ID", "Date", "Time", "Gross Sales", "Item" },
        ["toast"]  = new[] { "Order Id", "Sent Date", "Menu Item", "Item Qty", "Item Gross Price" },
        ["clover"] = new[] { "Payment ID", "Created Time", "Item Name", "Quantity", "Price" },
    };

    public async Task<EtlPreview> PreviewAsync(Stream csv, string fileName)
    {
        using var reader = new StreamReader(csv);
        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true, MissingFieldFound = null, HeaderValidated = null };
        using var p = new CsvParser(reader, cfg);
        await p.ReadAsync();
        var headers = (p.Record ?? Array.Empty<string>()).ToList();
        var rows = new List<Dictionary<string,string>>();
        for (int i = 0; i < 5 && await p.ReadAsync(); i++)
        {
            var rec = p.Record ?? Array.Empty<string>();
            rows.Add(headers.Select((h, idx) => (h, v: idx < rec.Length ? rec[idx] : "")).ToDictionary(x => x.h, x => x.v));
        }
        var detected = SourceSignatures.FirstOrDefault(s => s.Value.All(x => headers.Any(h => h.Contains(x, StringComparison.OrdinalIgnoreCase)))).Key ?? "manual";
        var map = new Dictionary<string,string>();
        foreach (var h in headers)
        {
            if (h.Contains("date", StringComparison.OrdinalIgnoreCase) || h.Contains("time", StringComparison.OrdinalIgnoreCase)) map[h] = "OccurredAt";
            else if (h.Contains("item", StringComparison.OrdinalIgnoreCase) || h.Contains("menu", StringComparison.OrdinalIgnoreCase)) map[h] = "MenuItem";
            else if (h.Contains("qty", StringComparison.OrdinalIgnoreCase) || h.Contains("quantity", StringComparison.OrdinalIgnoreCase)) map[h] = "Quantity";
            else if (h.Contains("price", StringComparison.OrdinalIgnoreCase) || h.Contains("total", StringComparison.OrdinalIgnoreCase) || h.Contains("sales", StringComparison.OrdinalIgnoreCase)) map[h] = "LineTotal";
            else map[h] = "(ignore)";
        }
        return new EtlPreview(detected, headers, rows, map);
    }

    public async Task<int> ImportAsync(Stream csv, int businessId, int locationId, Dictionary<string,string> mapping)
    {
        var menu = await db.MenuItems.Where(m => m.BusinessId == businessId).ToDictionaryAsync(m => m.Name.ToLowerInvariant());
        using var reader = new StreamReader(csv);
        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true, MissingFieldFound = null, HeaderValidated = null };
        using var p = new CsvParser(reader, cfg);
        await p.ReadAsync();
        var headers = (p.Record ?? Array.Empty<string>()).ToList();
        var idx = headers.Select((h, i) => (h, i)).ToDictionary(x => x.h, x => x.i);
        var toAdd = new List<Transaction>();
        while (await p.ReadAsync())
        {
            var rec = p.Record!;
            IEnumerable<string> GetAll(string field) => mapping
                .Where(kv => kv.Value == field && idx.TryGetValue(kv.Key, out var i) && i < rec.Length)
                .Select(kv => rec[idx[kv.Key]].Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value));

            var dateValue = string.Join(" ", GetAll("OccurredAt"));
            if (!DateTime.TryParse(dateValue, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var when)
                && !DateTime.TryParse(dateValue, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out when)) continue;

            var name = GetAll("MenuItem").FirstOrDefault() ?? "";
            if (string.IsNullOrWhiteSpace(name)) continue;
            int qty = int.TryParse(GetAll("Quantity").FirstOrDefault(), out var q) && q > 0 ? q : 1;
            var totalText = GetAll("LineTotal").FirstOrDefault() ?? "";
            bool hasTotal = decimal.TryParse(totalText, NumberStyles.Currency, CultureInfo.InvariantCulture, out var total)
                || decimal.TryParse(totalText, NumberStyles.Currency, CultureInfo.CurrentCulture, out total);

            var menuKey = name.Trim().ToLowerInvariant();
            if (!menu.TryGetValue(menuKey, out var item))
            {
                item = new MenuItem
                {
                    BusinessId = businessId,
                    Name = name.Trim(),
                    Category = "Imported",
                    Price = hasTotal ? total / qty : 0,
                    Cost = 0
                };
                db.MenuItems.Add(item);
                menu[menuKey] = item;
            }
            if (!hasTotal) total = item.Price * qty;
            toAdd.Add(new Transaction { BusinessId = businessId, LocationId = locationId, MenuItemId = item.Id, OccurredAt = when, Quantity = qty, LineTotal = total, Source = "upload" });
        }

        if (toAdd.Count == 0) return 0;

        // The generated records exist only to make a new installation demonstrable.
        // Once real data arrives, remove them so screens represent the restaurant's data.
        if (!await db.Transactions.AnyAsync(t => t.BusinessId == businessId && t.Source == "upload"))
        {
            var demoTransactions = await db.Transactions
                .Where(t => t.BusinessId == businessId && t.Source == "seed")
                .ToListAsync();
            db.Transactions.RemoveRange(demoTransactions);
        }

        await db.SaveChangesAsync();
        db.Transactions.AddRange(toAdd);
        await db.SaveChangesAsync();
        return toAdd.Count;
    }
}
