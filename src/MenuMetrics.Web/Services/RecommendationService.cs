using MenuMetrics.Web.Data;
using MenuMetrics.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
namespace MenuMetrics.Web.Services;
public class RecommendationService(ApplicationDbContext db, IProfitabilityService prof, IDemandService demand, IStaffingService staff) : IRecommendationService
{
    public async Task<IReadOnlyList<Recommendation>> GenerateAsync(int businessId)
    {
        var recs = new List<Recommendation>();
        var quads = await prof.ClassifyAsync(businessId);
        foreach (var d in quads.Where(q => q.Quadrant == "Dog"))
            recs.Add(new Recommendation { BusinessId = businessId, Title = $"Consider removing or repricing '{d.Name}'", Category = "menu", Detail = $"Low volume ({d.Volume}) and low margin ({d.Margin:C}). Candidate for menu rationalization.", ImpactScore = 0.6m, EstimatedRevenueLift = 250 });
        foreach (var s in quads.Where(q => q.Quadrant == "Star").Take(2))
            recs.Add(new Recommendation { BusinessId = businessId, Title = $"Feature '{s.Name}' in next promo", Category = "promo", Detail = "Already a star performer — bundle to increase basket size.", ImpactScore = 0.8m, EstimatedRevenueLift = 800 });

        var heat = await demand.BuildHeatmapAsync(businessId);
        var plan = staff.PlanFromHeatmap(heat);
        for (int d = 0; d < 7; d++) for (int h = 0; h < 24; h++)
            if (plan[d][h] >= 4)
                recs.Add(new Recommendation { BusinessId = businessId, Title = $"Schedule {plan[d][h]} staff on {(DayOfWeek)d} {h}:00", Category = "staffing", Detail = $"Forecast {heat[d][h]} orders/hr peak.", ImpactScore = 0.7m, EstimatedRevenueLift = 0 });

        var lowStock = await db.InventoryItems.Where(i => i.BusinessId == businessId && i.OnHand < i.ReorderPoint).ToListAsync();
        foreach (var i in lowStock)
            recs.Add(new Recommendation { BusinessId = businessId, Title = $"Reorder {i.Name}", Category = "inventory", Detail = $"On-hand {i.OnHand} {i.Unit} below reorder point {i.ReorderPoint}.", ImpactScore = 0.9m, EstimatedRevenueLift = 0 });

        return recs.OrderByDescending(r => r.ImpactScore).Take(12).ToList();
    }
}
