using MenuMetrics.Web.Data;
using MenuMetrics.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MenuMetrics.Web.Controllers;

[Authorize]
public class DashboardController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        int bid = await TenantHelper.GetBusinessIdAsync(db);

        // Load confirmed CSV-import sales. Aggregations happen in memory because
        // SQLite does not support Sum(decimal) through this EF Core query.
        var uploadedSales = await db.Transactions
            .AsNoTracking()
            .Where(t => t.BusinessId == bid && t.Source == "csv-import")
            .Select(t => new
            {
                t.OccurredAt,
                t.LineTotal
            })
            .ToListAsync();

        DateTime? firstTransaction = uploadedSales.Count == 0
            ? null
            : uploadedSales.Min(t => t.OccurredAt);

        DateTime? latestTransaction = uploadedSales.Count == 0
            ? null
            : uploadedSales.Max(t => t.OccurredAt);

        var totalRevenue = uploadedSales.Sum(t => t.LineTotal);

        var dailyRevenue = uploadedSales
            .GroupBy(t => t.OccurredAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyPoint(g.Key, g.Sum(t => t.LineTotal)))
            .ToList();

        var hasUploadedSales = uploadedSales.Count > 0;
        var hasCosts = await db.MenuItems
            .AnyAsync(m => m.BusinessId == bid && m.Cost > 0);
        var hasInventory = await db.InventoryItems
            .AnyAsync(i => i.BusinessId == bid);
        var hasPromotions = await db.Promotions
            .AnyAsync(p => p.BusinessId == bid);

        var historyDays = firstTransaction is null || latestTransaction is null
            ? 0
            : (int)(latestTransaction.Value.Date - firstTransaction.Value.Date).TotalDays + 1;

        var vm = new DashboardVm
        {
            RevenueAllTime = totalRevenue,
            ImportedLinesAllTime = uploadedSales.Count,
            AverageSaleLineAllTime = uploadedSales.Count > 0
                ? totalRevenue / uploadedSales.Count
                : 0,

            DailyRevenue = dailyRevenue,
            HistoryDays = historyDays,

            Readiness = new()
            {
                new(
                    "Sales and demand",
                    hasUploadedSales ? "Ready" : "Needs data",
                    hasUploadedSales
                        ? "Uploaded transaction history is available for trends and forecasts."
                        : "Confirm a POS transaction upload to unlock sales and demand insights.",
                    hasUploadedSales),

                new(
                    "Menu profitability",
                    hasCosts ? "Ready" : "Needs cost data",
                    hasCosts
                        ? "Menu prices and costs are available for gross-margin analysis."
                        : "Add menu-item costs or recipe costs before relying on profitability results.",
                    hasCosts),

                new(
                    "Inventory planning",
                    hasInventory ? "Partial" : "Needs data",
                    hasInventory
                        ? "Recorded stock is available. Recipe-based usage is not yet configured."
                        : "Add inventory and recipe information to assess expected ingredient usage.",
                    hasInventory),

                new(
                    "Promotion analysis",
                    hasPromotions ? "Ready" : "Needs data",
                    hasPromotions
                        ? "Promotion windows are available for before/during/after comparison."
                        : "Add promotion periods to assess promotion performance.",
                    hasPromotions),

                new(
                    "Staffing analysis",
                    "Estimate only",
                    "Recommendations use demand-based staffing rules. Upload schedules to compare planned coverage with demand.",
                    false)
            }
        };

        return View(vm);
    }
}
