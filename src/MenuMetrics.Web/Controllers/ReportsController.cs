using MenuMetrics.Web.Data;
using MenuMetrics.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace MenuMetrics.Web.Controllers;
[Authorize]
public class ReportsController(IRecommendationService recs, ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await recs.GenerateAsync(await TenantHelper.GetBusinessIdAsync(db)));
}
