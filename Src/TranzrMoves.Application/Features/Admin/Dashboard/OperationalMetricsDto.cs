namespace TranzrMoves.Application.Features.Admin.Dashboard;

public class OperationalMetricsDto
{
    public decimal AverageQuoteValue { get; set; }
    public int QuotesCompletedThisMonth { get; set; }
    public double AverageCompletionTime { get; set; }
    public double CustomerSatisfactionScore { get; set; }
}
