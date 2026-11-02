using Microsoft.AspNetCore.Mvc.Rendering;

namespace MenuMetrics.Web.Models.ViewModels;

public class ImportIndexVm
{
    public int? LocationId { get; set; }
    public List<SelectListItem> Locations { get; set; } = new();
    public string? Error { get; set; }
}

public class ColumnMappingInput
{
    public string SourceColumn { get; set; } = "";
    public string TargetField { get; set; } = "Ignore";
}

public class ImportMappingVm
{
    public int RunId { get; set; }
    public string OriginalFileName { get; set; } = "";
    public string DetectedSource { get; set; } = "manual";
    public List<string> Headers { get; set; } = new();
    public List<List<string>> SampleRows { get; set; } = new();
    public List<ColumnMappingInput> Mappings { get; set; } = new();
    public string? Error { get; set; }
}

public class ImportConfirmationVm
{
    public int RunId { get; set; }
    public string OriginalFileName { get; set; } = "";
    public string LocationName { get; set; } = "";
    public string DetectedSource { get; set; } = "manual";
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int RejectedRows { get; set; }
    public List<string> Issues { get; set; } = new();
    public bool CanConfirm => ValidRows > 0;
}

public class ImportSummaryVm
{
    public int Id { get; set; }
    public string OriginalFileName { get; set; } = "";
    public string LocationName { get; set; } = "";
    public string DetectedSource { get; set; } = "manual";
    public string Status { get; set; } = "";
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int RejectedRows { get; set; }
    public int ImportedRows { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public List<string> Issues { get; set; } = new();
}

public static class ImportTargetFields
{
    public static readonly string[] All =
        { "Ignore", "Date", "Time", "MenuItem", "Quantity", "LineTotal" };
}