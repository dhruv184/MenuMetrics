using MenuMetrics.Web.Models.Entities;
namespace MenuMetrics.Web.Services;
public interface IPromotionService { Task<IReadOnlyList<Promotion>> ListAsync(int businessId); Task RecomputeAsync(int businessId); }
