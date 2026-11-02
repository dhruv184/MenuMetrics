namespace MenuMetrics.Web.Models.Entities;
public class Business
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Timezone { get; set; } = "America/New_York";
    public string Plan { get; set; } = "Starter";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Location> Locations { get; set; } = new();
    public List<MenuItem> MenuItems { get; set; } = new();
}
