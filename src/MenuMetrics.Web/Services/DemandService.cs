using MenuMetrics.Web.Data;
using Microsoft.EntityFrameworkCore;
namespace MenuMetrics.Web.Services;
public class DemandService(ApplicationDbContext db) : IDemandService
{
    public async Task<int[][]> BuildHeatmapAsync(int businessId)
    {
        var since = DateTime.UtcNow.AddDays(-28);
        var grid = new int[7][];
        for (int i = 0; i < 7; i++) grid[i] = new int[24];
        var txs = await db.Transactions.Where(t => t.BusinessId == businessId && t.OccurredAt >= since)
            .Select(t => new { t.OccurredAt }).ToListAsync();
        foreach (var t in txs) grid[(int)t.OccurredAt.DayOfWeek][t.OccurredAt.Hour]++;
        // Average per 4 weeks
        for (int d = 0; d < 7; d++) for (int h = 0; h < 24; h++) grid[d][h] = grid[d][h] / 4;
        return grid;
    }
}
