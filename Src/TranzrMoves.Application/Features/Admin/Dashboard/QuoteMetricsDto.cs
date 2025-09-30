namespace TranzrMoves.Application.Features.Admin.Dashboard;

public class QuoteMetricsDto
{
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Paid { get; set; }
    public int PartiallyPaid { get; set; }
    public int Succeeded { get; set; }
    public int Cancelled { get; set; }
    public QuoteTypeBreakdownDto ByType { get; set; } = new();
}

public class QuoteTypeBreakdownDto
{
    public int Send { get; set; }
    public int Receive { get; set; }
    public int Removals { get; set; }
}
