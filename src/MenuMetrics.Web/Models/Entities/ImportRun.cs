using System.ComponentModel.DataAnnotations;

namespace MenuMetrics.Web.Models.Entities;

public class ImportRun
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public int LocationId { get; set; }
    [MaxLength(450)] public string UserId { get; set; } = "";
    [MaxLength(255)] public string OriginalFileName { get; set; } = "";
    // Private server-side staging path; never expose this in a view/API.
    [MaxLength(1000)] public string StoredFilePath { get; set; } = "";
    [MaxLength(40)] public string DetectedSource { get; set; } = "manual";
    [MaxLength(40)] public string Status { get; set; } = "AwaitingMapping";
    public string MappingJson { get; set; } = "[]";
    public string ValidationIssuesJson { get; set; } = "[]";
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int RejectedRows { get; set; }
    public int ImportedRows { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public List<Transaction> Transactions { get; set; } = new();
    public Location? Location { get; set; }
}