namespace MenuMetrics.Web.Services;

public sealed record ClearSalesDataResult(
    int TransactionsDeleted,
    int ImportRunsDeleted,
    int StagedFilesRemoved,
    int StagedFilesNotRemoved);