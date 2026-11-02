using MenuMetrics.Web.Data;
using MenuMetrics.Web.Models.Entities;
using MenuMetrics.Web.Models.ViewModels;
using MenuMetrics.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MenuMetrics.Web.Controllers;

[Authorize]
public class UploadController : Controller
{
    private const long MaxUploadBytes = 10 * 1024 * 1024;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICsvImportWorkflowService _imports;

    public UploadController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ICsvImportWorkflowService imports)
    {
        _db = db;
        _userManager = userManager;
        _imports = imports;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var scope = await GetScopeAsync();
        if (scope == null) return Forbid();
        return View(await BuildIndexVmAsync(scope.Value.BusinessId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxUploadBytes + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes + 64 * 1024)]
    public async Task<IActionResult> Upload(IFormFile? file, int locationId)
    {
        var scope = await GetScopeAsync();
        if (scope == null) return Forbid();
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Choose a non-empty CSV file.";
            return RedirectToAction(nameof(Index));
        }
        if (file.Length > MaxUploadBytes)
        {
            TempData["Error"] = "The CSV must be 10 MB or smaller.";
            return RedirectToAction(nameof(Index));
        }
        if (!string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Upload a .csv file.";
            return RedirectToAction(nameof(Index));
        }
        if (!await _db.Locations.AnyAsync(x => x.Id == locationId && x.BusinessId == scope.Value.BusinessId))
            return Forbid();

        try
        {
            await using var stream = file.OpenReadStream();
            var vm = await _imports.CreateDraftAsync(
                stream, file.FileName, scope.Value.BusinessId, locationId, scope.Value.UserId);
            return View("Map", vm);
        }
        catch (InvalidDataException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Map(int runId, List<ColumnMappingInput> mappings)
    {
        var scope = await GetScopeAsync();
        if (scope == null) return Forbid();
        try
        {
            var vm = await _imports.MapAndValidateAsync(
                runId, scope.Value.BusinessId, scope.Value.UserId, mappings ?? new());
            return View("Confirm", vm);
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or InvalidDataException)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Confirm(int id)
    {
        var scope = await GetScopeAsync();
        if (scope == null) return Forbid();
        try
        {
            return View(await _imports.GetConfirmationAsync(id, scope.Value.BusinessId, scope.Value.UserId));
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or InvalidDataException)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Commit(int runId, bool confirmValidRows)
    {
        var scope = await GetScopeAsync();
        if (scope == null) return Forbid();
        if (!confirmValidRows)
        {
            TempData["Error"] = "Confirm that only valid rows will be imported.";
            return RedirectToAction(nameof(Confirm), new { id = runId });
        }
        try
        {
            var result = await _imports.CommitAsync(runId, scope.Value.BusinessId, scope.Value.UserId);
            return View("Complete", result);
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or InvalidDataException)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Confirm), new { id = runId });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Summary(int id)
    {
        var scope = await GetScopeAsync();
        if (scope == null) return Forbid();
        var summary = await _imports.GetSummaryAsync(id, scope.Value.BusinessId, scope.Value.UserId);
        return summary == null ? NotFound() : View("Complete", summary);
    }

    [HttpGet]
    public async Task<IActionResult> History()
    {
        var scope = await GetScopeAsync();
        if (scope == null) return Forbid();
        return View(await _imports.GetHistoryAsync(scope.Value.BusinessId, scope.Value.UserId));
    }

    private async Task<(int BusinessId, string UserId)?> GetScopeAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.BusinessId is int businessId
            ? (businessId, user.Id)
            : null;
    }

    private async Task<ImportIndexVm> BuildIndexVmAsync(int businessId)
    {
        var locations = await _db.Locations.AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync();
        return new ImportIndexVm
        {
            Locations = locations.Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(x.Name, x.Id.ToString())).ToList()
        };
    }
    [Authorize(Roles = "Owner,Admin")]
    [HttpGet]
    public async Task<IActionResult> ClearSalesData()
    {
        var scope = await GetScopeAsync();
        if (scope == null) return Forbid();

        return View();
    }

    [Authorize(Roles = "Owner,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearSalesDataConfirmed(string? confirmation)
    {
        var scope = await GetScopeAsync();
        if (scope == null) return Forbid();

        if (!string.Equals(confirmation?.Trim(), "CLEAR SALES", StringComparison.Ordinal))
        {
            ViewData["ErrorMessage"] = "Type CLEAR SALES exactly to confirm.";
            return View("ClearSalesData");
        }

        var result = await _imports.ClearBusinessSalesAsync(scope.Value.BusinessId);

        TempData["Success"] =
            $"Cleared {result.TransactionsDeleted:N0} transactions and " +
            $"{result.ImportRunsDeleted:N0} import runs. Other business data was preserved.";

        if (result.StagedFilesNotRemoved > 0)
        {
            TempData["Warning"] =
                $"{result.StagedFilesNotRemoved:N0} staged file(s) could not be removed. " +
                "The database records were still cleared.";
        }

        return RedirectToAction(nameof(History));
    }
  
}