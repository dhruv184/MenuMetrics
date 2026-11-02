namespace MenuMetrics.Web.Models.Entities;
public class Promotion
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public string Name { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal DiscountPct { get; set; }
    public decimal? PreRevenue { get; set; }
    public decimal? DuringRevenue { get; set; }
    public decimal? PostRevenue { get; set; }
    public decimal? Roi { get; set; }
    public string? Rating { get; set; }
}
