using MenuMetrics.Web.Models.Entities;
namespace MenuMetrics.Web.Services;
public interface IInventoryService { Task<IReadOnlyList<InventoryItem>> ListAsync(int bid); }
public class InventoryService : IInventoryService
{
    private readonly MenuMetrics.Web.Data.ApplicationDbContext _db;
    public InventoryService(MenuMetrics.Web.Data.ApplicationDbContext db) { _db = db; }
    public async Task<IReadOnlyList<InventoryItem>> ListAsync(int bid) =>
        await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            System.Linq.Queryable.OrderBy(System.Linq.Queryable.Where(_db.InventoryItems, i => i.BusinessId == bid), i => i.Name));
}
