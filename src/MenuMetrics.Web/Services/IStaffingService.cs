namespace MenuMetrics.Web.Services;
public interface IStaffingService { int StaffFor(int ordersPerHour); IReadOnlyList<(int lo,int hi,int staff)> Rules { get; } int[][] PlanFromHeatmap(int[][] heatmap); }
