using NodaTime;

namespace TranzrMoves.Application.Contracts;

/// <summary>
/// KPI summary for the Business Dashboard, scoped to the authenticated Business Account.
/// </summary>
public sealed class DashboardSummaryDto
{
    public int UpcomingJobs { get; init; }
    public int JobsInProgress { get; init; }
    public int CompletedJobsThisMonth { get; init; }
    public decimal OutstandingInvoiceAmount { get; init; }
}

public sealed class UpcomingJobDto
{
    public string BookingReference { get; init; } = string.Empty;
    public string? JobType { get; init; }
    public string? PickupLocation { get; init; }
    public string? DropoffLocation { get; init; }
    public Instant ScheduledDate { get; init; }
    public string? Status { get; init; }
}

public sealed class ActiveJobDto
{
    public string BookingReference { get; init; } = string.Empty;
    public string? Status { get; init; }
    public Instant? Eta { get; init; }
}

public sealed class OutstandingInvoiceDto
{
    public string InvoiceNumber { get; init; } = string.Empty;
    public LocalDate InvoiceDate { get; init; }
    public LocalDate DueDate { get; init; }
    public decimal AmountOutstanding { get; init; }
}

public sealed class RecentActivityDto
{
    public Instant Timestamp { get; init; }
    public string? EventType { get; init; }
    public string? Description { get; init; }
}
