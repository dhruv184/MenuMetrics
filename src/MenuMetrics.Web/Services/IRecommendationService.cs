using MenuMetrics.Web.Models.Entities;
namespace MenuMetrics.Web.Services;
public interface IRecommendationService { Task<IReadOnlyList<Recommendation>> GenerateAsync(int businessId); }
