namespace MenuMetrics.Web.Services;
public class StaffingService : IStaffingService
{
    public IReadOnlyList<(int lo, int hi, int staff)> Rules { get; } = new List<(int,int,int)>
    { (0,20,1), (21,50,2), (51,100,4), (101,150,6), (151,9999,8) };
    public int StaffFor(int orders) => Rules.First(r => orders >= r.lo && orders <= r.hi).staff;
    public int[][] PlanFromHeatmap(int[][] heatmap)
    {
        var plan = new int[heatmap.Length][];
        for (int d = 0; d < heatmap.Length; d++) {
            plan[d] = new int[heatmap[d].Length];
            for (int h = 0; h < heatmap[d].Length; h++) plan[d][h] = StaffFor(heatmap[d][h]);
        }
        return plan;
    }
}
