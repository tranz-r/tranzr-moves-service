namespace TranzrMoves.Application.Features.Admin.Dashboard;

public class DriverMetricsDto
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Available { get; set; }
    public int Busy { get; set; }
    public double UtilizationRate { get; set; }
    public double AverageAssignmentsPerDriver { get; set; }
}
