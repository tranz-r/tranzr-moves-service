namespace TranzrMoves.Application.Features.Admin.Dashboard;

public class UserMetricsDto
{
    public int Total { get; set; }
    public int Customers { get; set; }
    public int Drivers { get; set; }
    public int Admins { get; set; }
    public int NewThisMonth { get; set; }
    public double GrowthRate { get; set; }
}
