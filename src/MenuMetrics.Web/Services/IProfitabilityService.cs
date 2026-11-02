namespace MenuMetrics.Web.Services;
public interface IProfitabilityService { Task<IReadOnlyList<MenuQuadrant>> ClassifyAsync(int businessId); }
public record MenuQuadrant(int MenuItemId, string Name, decimal Volume, decimal Margin, string Quadrant);
