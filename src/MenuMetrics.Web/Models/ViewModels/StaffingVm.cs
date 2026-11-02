namespace MenuMetrics.Web.Models.ViewModels;
public class StaffingVm
{
    public int[][] Heatmap { get; set; } = Array.Empty<int[]>();
    public int[][] Plan { get; set; } = Array.Empty<int[]>();
    public List<(int lo,int hi,int staff)> Rules { get; set; } = new();
}
