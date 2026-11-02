using MenuMetrics.Web.Data;
using MenuMetrics.Web.Services;
using MenuMetrics.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace MenuMetrics.Web.Controllers;
[Authorize]
public class StaffingController(IDemandService demand, IStaffingService staff, ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        int bid = await TenantHelper.GetBusinessIdAsync(db);
        var heat = await demand.BuildHeatmapAsync(bid);
        return View(new StaffingVm { Heatmap = heat, Plan = staff.PlanFromHeatmap(heat), Rules = staff.Rules.ToList() });
    }
}
