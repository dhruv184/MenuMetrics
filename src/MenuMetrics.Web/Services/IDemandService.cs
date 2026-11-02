namespace MenuMetrics.Web.Services;
public interface IDemandService { Task<int[][]> BuildHeatmapAsync(int businessId); }
