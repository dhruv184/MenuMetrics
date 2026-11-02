using MenuMetrics.Web.Models.ViewModels;

namespace MenuMetrics.Web.Services;

public interface ICsvImportWorkflowService
{
    Task<ImportMappingVm> CreateDraftAsync(
        Stream csv, string originalFileName, int businessId, int locationId, string userId);

    Task<ImportMappingVm> GetMappingAsync(int runId, int businessId, string userId);

    Task<ImportConfirmationVm> MapAndValidateAsync(
        int runId, int businessId, string userId, List<ColumnMappingInput> mappings);

    Task<ImportConfirmationVm> GetConfirmationAsync(int runId, int businessId, string userId);

    Task<ImportSummaryVm> CommitAsync(int runId, int businessId, string userId);

    Task<ImportSummaryVm?> GetSummaryAsync(int runId, int businessId, string userId);

    Task<List<ImportSummaryVm>> GetHistoryAsync(int businessId, string userId);

    Task<ClearSalesDataResult> ClearBusinessSalesAsync(int businessId);
}