namespace MenuMetrics.Web.Models.Entities;
public class Forecast
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public int? MenuItemId { get; set; }
    public DateTime ForDate { get; set; }
    public decimal PredictedOrders { get; set; }
    public decimal BaseDemand { get; set; }
    public decimal SeasonalityFactor { get; set; }
    public decimal TrendFactor { get; set; }
    public decimal Lower95 { get; set; }
    public decimal Upper95 { get; set; }
    public string Model { get; set; } = "prophet";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
