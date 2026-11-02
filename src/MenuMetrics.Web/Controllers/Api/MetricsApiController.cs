using MenuMetrics.Web.Data;
using MenuMetrics.Web.Services;
using Microsoft.AspNetCore.Mvc;
namespace MenuMetrics.Web.Controllers.Api;
[ApiController, Route("api/metrics")]
public class MetricsApiController(ApplicationDbContext db, IDemandService demand, IForecastService fc) : ControllerBase
{
    [HttpGet("heatmap")] public async Task<IActionResult> Heatmap() => Ok(await demand.BuildHeatmapAsync(await TenantHelper.GetBusinessIdAsync(db)));
    [HttpGet("forecast")] public async Task<IActionResult> Forecast(int days = 14) => Ok(await fc.ForecastAsync(await TenantHelper.GetBusinessIdAsync(db), days));
}
