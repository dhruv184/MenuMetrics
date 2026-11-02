namespace MenuMetrics.Web.Models.Entities;
public class MenuItem
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public decimal Margin => Price - Cost;
    public bool Active { get; set; } = true;
}
