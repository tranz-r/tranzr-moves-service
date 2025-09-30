namespace TranzrMoves.Application.Features.Admin.Dashboard;

public class RevenueMetricsDto
{
    public decimal Total { get; set; }
    public decimal ThisMonth { get; set; }
    public decimal LastMonth { get; set; }
    public double MonthOverMonthGrowth { get; set; }
    public ServiceTypeRevenueDto ByServiceType { get; set; } = new();
}

public class ServiceTypeRevenueDto
{
    public decimal Send { get; set; }
    public decimal Receive { get; set; }
    public decimal Removals { get; set; }
}
