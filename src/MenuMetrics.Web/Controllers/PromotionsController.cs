using MenuMetrics.Web.Data;
using MenuMetrics.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace MenuMetrics.Web.Controllers;
[Authorize]
public class PromotionsController(IPromotionService svc, ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await svc.ListAsync(await TenantHelper.GetBusinessIdAsync(db)));
    [HttpPost] public async Task<IActionResult> Recompute()
    {
        await svc.RecomputeAsync(await TenantHelper.GetBusinessIdAsync(db));
        return RedirectToAction(nameof(Index));
    }
}
