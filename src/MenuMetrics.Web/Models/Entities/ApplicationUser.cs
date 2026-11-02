using Microsoft.AspNetCore.Identity;
namespace MenuMetrics.Web.Models.Entities;
public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public int? BusinessId { get; set; }
    public Business? Business { get; set; }
}
