namespace TranzrMoves.Application.Features.Admin.Dashboard;

public class PaymentMetricsDto
{
    public decimal TotalAmount { get; set; }
    public decimal CompletedAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public int TotalTransactions { get; set; }
    public double SuccessRate { get; set; }
    public decimal AverageOrderValue { get; set; }
    public PaymentTypeBreakdownDto ByPaymentType { get; set; } = new();
}

public class PaymentTypeBreakdownDto
{
    public int Full { get; set; }
    public int Deposit { get; set; }
    public int Later { get; set; }
    public int Balance { get; set; }
}
