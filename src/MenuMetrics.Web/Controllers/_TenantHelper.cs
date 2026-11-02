using MenuMetrics.Web.Data;
using Microsoft.EntityFrameworkCore;
namespace MenuMetrics.Web.Controllers;
public static class TenantHelper
{
    public static async Task<int> GetBusinessIdAsync(ApplicationDbContext db) =>
        await db.Businesses.Select(b => b.Id).FirstAsync();
}
