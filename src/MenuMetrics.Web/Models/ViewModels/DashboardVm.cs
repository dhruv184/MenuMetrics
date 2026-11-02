namespace MenuMetrics.Web.Models.ViewModels;

public record DailyPoint(DateTime Date, decimal Revenue);
public record DataReadinessItem(string Area, string Status, string Detail, bool IsReady);

public class DashboardVm
{
    public decimal RevenueAllTime { get; set; }
    public int ImportedLinesAllTime { get; set; }
    public decimal AverageSaleLineAllTime { get; set; }
    public List<DailyPoint> DailyRevenue { get; set; } = new();
    public int HistoryDays { get; set; }
    public List<DataReadinessItem> Readiness { get; set; } = new();
}