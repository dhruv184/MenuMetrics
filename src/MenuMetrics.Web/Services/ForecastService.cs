using System.Net.Http.Json;
using MenuMetrics.Web.Data;
using Microsoft.EntityFrameworkCore;
namespace MenuMetrics.Web.Services;
public class ForecastService(ApplicationDbContext db, IHttpClientFactory http, IConfiguration cfg, ILogger<ForecastService> log) : IForecastService
{
    public async Task<IReadOnlyList<ForecastPoint>> ForecastAsync(int businessId, int days)
    {
        // Try ML microservice; fall back to seasonal-naive local model.
        try
        {
            var url = cfg["MLService:BaseUrl"];
            if (!string.IsNullOrEmpty(url))
            {
                var client = http.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                var hist = await db.Transactions.Where(t => t.BusinessId == businessId)
                    .GroupBy(t => t.OccurredAt.Date)
                    .Select(g => new { ds = g.Key, y = g.Sum(x => (decimal)x.Quantity) })
                    .ToListAsync();
                var resp = await client.PostAsJsonAsync($"{url}/forecast", new { horizon = days, history = hist });
                if (resp.IsSuccessStatusCode)
                {
                    var dto = await resp.Content.ReadFromJsonAsync<List<ForecastPoint>>();
                    if (dto is not null) return dto;
                }
            }
        }
        catch (Exception ex) { log.LogWarning(ex, "ML service unavailable, falling back to local model."); }

        // Local fallback: seasonal-naive (4-week average per weekday) + linear trend
        var since = DateTime.UtcNow.Date.AddDays(-56);
        var daily = await db.Transactions.Where(t => t.BusinessId == businessId && t.OccurredAt >= since)
            .GroupBy(t => t.OccurredAt.Date).Select(g => new { d = g.Key, q = g.Sum(x => x.Quantity) }).ToListAsync();
        var byDow = daily.GroupBy(x => (int)x.d.DayOfWeek).ToDictionary(g => g.Key, g => (decimal)g.Average(x => x.q));
        var avg = daily.Count > 0 ? (decimal)daily.Average(x => x.q) : 50m;
        var slope = 0.005m * avg;
        var list = new List<ForecastPoint>();
        for (int i = 1; i <= days; i++)
        {
            var d = DateTime.UtcNow.Date.AddDays(i);
            var seasonal = byDow.TryGetValue((int)d.DayOfWeek, out var s) ? s : avg;
            var trend = slope * i;
            var baseDemand = avg;
            var pred = Math.Max(0, baseDemand + (seasonal - avg) + trend);
            list.Add(new ForecastPoint(d, decimal.Round(pred, 1), decimal.Round(pred * 0.85m, 1), decimal.Round(pred * 1.15m, 1), decimal.Round(baseDemand,1), decimal.Round(seasonal - avg, 1), decimal.Round(trend, 1)));
        }
        return list;
    }
}
