namespace MenuMetrics.Web.Models.Entities;
public class InventoryItem
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal OnHand { get; set; }
    public decimal ReorderPoint { get; set; }
    public string Unit { get; set; } = "ea";
}
