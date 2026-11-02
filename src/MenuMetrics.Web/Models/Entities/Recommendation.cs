namespace MenuMetrics.Web.Models.Entities;
public class Recommendation
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public string Title { get; set; } = "";
    public string Category { get; set; } = ""; // staffing, menu, promo, inventory
    public string Detail { get; set; } = "";
    public decimal ImpactScore { get; set; }
    public decimal EstimatedRevenueLift { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "new";
}
