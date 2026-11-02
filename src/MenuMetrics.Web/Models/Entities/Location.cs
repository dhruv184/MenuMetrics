namespace MenuMetrics.Web.Models.Entities;
public class Location
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public Business? Business { get; set; }
    public string Name { get; set; } = "";
    public string? Address { get; set; }
}
