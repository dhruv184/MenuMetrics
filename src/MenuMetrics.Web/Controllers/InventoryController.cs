using MenuMetrics.Web.Data;
using MenuMetrics.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace MenuMetrics.Web.Controllers;
[Authorize]
public class InventoryController(IInventoryService svc, ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await svc.ListAsync(await TenantHelper.GetBusinessIdAsync(db)));
}
