using MenuMetrics.Web.Data;
using Microsoft.EntityFrameworkCore;
namespace MenuMetrics.Web.Services;
public class ProfitabilityService(ApplicationDbContext db) : IProfitabilityService
{
    public async Task<IReadOnlyList<MenuQuadrant>> ClassifyAsync(int businessId)
    {
        var since = DateTime.UtcNow.AddDays(-30);
        var rows = await db.Transactions
            .Where(t => t.BusinessId == businessId && t.OccurredAt >= since)
            .GroupBy(t => new { t.MenuItemId, t.MenuItem!.Name, t.MenuItem.Price, t.MenuItem.Cost })
            .Select(g => new {
                g.Key.MenuItemId, g.Key.Name,
                Volume = (decimal)g.Sum(x => x.Quantity),
                Margin = g.Key.Price - g.Key.Cost
            }).ToListAsync();
        if (rows.Count == 0) return Array.Empty<MenuQuadrant>();
        decimal vMed = rows.OrderBy(r => r.Volume).ElementAt(rows.Count / 2).Volume;
        decimal mMed = rows.OrderBy(r => r.Margin).ElementAt(rows.Count / 2).Margin;
        return rows.Select(r => {
            string q = (r.Volume >= vMed, r.Margin >= mMed) switch {
                (true, true) => "Star",
                (true, false) => "Cash Cow",
                (false, true) => "Question Mark",
                _ => "Dog"
            };
            return new MenuQuadrant(r.MenuItemId, r.Name, r.Volume, r.Margin, q);
        }).ToList();
    }
}
