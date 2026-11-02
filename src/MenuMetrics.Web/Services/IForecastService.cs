namespace MenuMetrics.Web.Services;
public interface IForecastService { Task<IReadOnlyList<ForecastPoint>> ForecastAsync(int businessId, int days); }
public record ForecastPoint(DateTime Date, decimal Predicted, decimal Lower, decimal Upper, decimal Base, decimal Seasonality, decimal Trend);
