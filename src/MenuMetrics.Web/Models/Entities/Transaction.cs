namespace MenuMetrics.Web.Models.Entities;
public class Transaction
{
    public long Id { get; set; }
    public int BusinessId { get; set; }
    public int LocationId { get; set; }
    public int MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }
    public DateTime OccurredAt { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public string? Source { get; set; } // square, toast, manual
    public int? ImportRunId { get; set; }
    public ImportRun? ImportRun { get; set; }
}
