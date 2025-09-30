namespace TranzrMoves.Application.Features.Admin.Dashboard;

public class DashboardMetricsDto
{
    public QuoteMetricsDto Quotes { get; set; } = new();
    public PaymentMetricsDto Payments { get; set; } = new();
    public UserMetricsDto Users { get; set; } = new();
    public DriverMetricsDto Drivers { get; set; } = new();
    public RevenueMetricsDto Revenue { get; set; } = new();
    public OperationalMetricsDto Operational { get; set; } = new();
    public DateTimeOffset LastUpdated { get; set; }
    public DateTimeOffset CacheExpiry { get; set; }
}
