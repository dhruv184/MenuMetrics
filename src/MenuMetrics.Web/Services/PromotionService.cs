using MenuMetrics.Web.Data;
using MenuMetrics.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
namespace MenuMetrics.Web.Services;
public class PromotionService(ApplicationDbContext db) : IPromotionService
{
    public async Task<IReadOnlyList<Promotion>> ListAsync(int bid) =>
        await db.Promotions.Where(p => p.BusinessId == bid).OrderByDescending(p => p.StartDate).ToListAsync();

    public async Task RecomputeAsync(int bid)
    {
        var promos = await db.Promotions.Where(p => p.BusinessId == bid).ToListAsync();
        foreach (var p in promos)
        {
            var span = (p.EndDate - p.StartDate).TotalDays;
            decimal Sum(DateTime s, DateTime e) =>
                db.Transactions.Where(t => t.BusinessId == bid && t.OccurredAt >= s && t.OccurredAt < e).Sum(t => (decimal?)t.LineTotal) ?? 0;
            p.PreRevenue    = Sum(p.StartDate.AddDays(-span), p.StartDate);
            p.DuringRevenue = Sum(p.StartDate, p.EndDate);
            p.PostRevenue   = Sum(p.EndDate, p.EndDate.AddDays(span));
            var lift = (p.DuringRevenue ?? 0) - (p.PreRevenue ?? 0);
            var cost = (p.DuringRevenue ?? 0) * (p.DiscountPct / 100m);
            p.Roi = cost > 0 ? lift / cost : 0;
            p.Rating = p.Roi >= 2 ? "Excellent" : p.Roi >= 1 ? "Good" : p.Roi >= 0.3m ? "Neutral" : "Poor";
        }
        await db.SaveChangesAsync();
    }
}
