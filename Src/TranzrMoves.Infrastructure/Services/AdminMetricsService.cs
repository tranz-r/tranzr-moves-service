using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Features.Admin.Dashboard;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Services;

public class AdminMetricsService : IAdminMetricsService
{
    private readonly TranzrMovesDbContext _context;
    private readonly ILogger<AdminMetricsService> _logger;

    public AdminMetricsService(TranzrMovesDbContext context, ILogger<AdminMetricsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ErrorOr<DashboardMetricsDto>> GetDashboardMetricsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        try
        {
            // Execute queries sequentially to avoid Entity Framework thread safety issues
            // DbContext is not thread-safe and cannot be used concurrently
            var quotes = await GetQuoteMetricsAsync(fromDate, toDate, ct);
            var payments = await GetPaymentMetricsAsync(fromDate, toDate, ct);
            var users = await GetUserMetricsAsync(fromDate, toDate, ct);
            var drivers = await GetDriverMetricsAsync(fromDate, toDate, ct);
            var revenue = await GetRevenueMetricsAsync(fromDate, toDate, ct);
            var operational = await GetOperationalMetricsAsync(fromDate, toDate, ct);

            // Combine results into DTO
            return new DashboardMetricsDto
            {
                Quotes = quotes,
                Payments = payments,
                Users = users,
                Drivers = drivers,
                Revenue = revenue,
                Operational = operational,
                LastUpdated = DateTimeOffset.UtcNow,
                CacheExpiry = DateTimeOffset.UtcNow.AddMinutes(5)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard metrics");
            return Error.Unexpected("DashboardMetrics.Failed", "Failed to retrieve dashboard metrics.");
        }
    }

    private async Task<QuoteMetricsDto> GetQuoteMetricsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var query = _context.Set<Quote>().AsNoTracking().AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(q => q.CreatedAt >= fromDate.Value.ToUniversalTime());
        if (toDate.HasValue)
            query = query.Where(q => q.CreatedAt <= toDate.Value.ToUniversalTime());

        var quotes = await query.ToListAsync(ct);

        return new QuoteMetricsDto
        {
            Total = quotes.Count,
            Pending = quotes.Count(q => q.PaymentStatus == PaymentStatus.Pending),
            Paid = quotes.Count(q => q.PaymentStatus == PaymentStatus.Paid),
            PartiallyPaid = quotes.Count(q => q.PaymentStatus == PaymentStatus.PartiallyPaid),
            Succeeded = quotes.Count(q => q.PaymentStatus == PaymentStatus.Succeeded),
            Cancelled = quotes.Count(q => q.PaymentStatus == PaymentStatus.Cancelled),
            ByType = new QuoteTypeBreakdownDto
            {
                Send = quotes.Count(q => q.Type == QuoteType.Send),
                Receive = quotes.Count(q => q.Type == QuoteType.Receive),
                Removals = quotes.Count(q => q.Type == QuoteType.Removals)
            }
        };
    }

    private async Task<PaymentMetricsDto> GetPaymentMetricsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var query = _context.Set<Quote>()
            .AsNoTracking()
            .Where(q => q.TotalCost.HasValue && q.TotalCost > 0)
            .AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(q => q.CreatedAt >= fromDate.Value.ToUniversalTime());
        if (toDate.HasValue)
            query = query.Where(q => q.CreatedAt <= toDate.Value.ToUniversalTime());

        var quotes = await query.ToListAsync(ct);

        var totalAmount = quotes.Sum(q => q.TotalCost ?? 0);
        var completedAmount = quotes
            .Where(q => q.PaymentStatus == PaymentStatus.Paid || q.PaymentStatus == PaymentStatus.Succeeded)
            .Sum(q => q.TotalCost ?? 0);
        var pendingAmount = quotes
            .Where(q => q.PaymentStatus == PaymentStatus.Pending || q.PaymentStatus == PaymentStatus.PartiallyPaid)
            .Sum(q => q.TotalCost ?? 0);

        var successfulTransactions = quotes.Count(q =>
            q.PaymentStatus == PaymentStatus.Paid || q.PaymentStatus == PaymentStatus.Succeeded);

        return new PaymentMetricsDto
        {
            TotalAmount = totalAmount,
            CompletedAmount = completedAmount,
            PendingAmount = pendingAmount,
            TotalTransactions = quotes.Count,
            SuccessRate = quotes.Count > 0 ? (double)successfulTransactions / quotes.Count * 100 : 0,
            AverageOrderValue = quotes.Count > 0 ? quotes.Average(q => q.TotalCost ?? 0) : 0,
            ByPaymentType = new PaymentTypeBreakdownDto
            {
                Full = quotes.Count(q => q.PaymentType == PaymentType.Full),
                Deposit = quotes.Count(q => q.PaymentType == PaymentType.Deposit),
                Later = quotes.Count(q => q.PaymentType == PaymentType.Later),
                Balance = quotes.Count(q => q.PaymentType == PaymentType.Balance)
            }
        };
    }

    private async Task<UserMetricsDto> GetUserMetricsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var query = _context.Set<User>().AsNoTracking().AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(u => u.CreatedAt >= fromDate.Value.ToUniversalTime());
        if (toDate.HasValue)
            query = query.Where(u => u.CreatedAt <= toDate.Value.ToUniversalTime());

        var users = await query.ToListAsync(ct);
        var thisMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        return new UserMetricsDto
        {
            Total = users.Count,
            Customers = users.Count(u => u.Role == Role.customer),
            Drivers = users.Count(u => u.Role == Role.driver),
            Admins = users.Count(u => u.Role == Role.admin),
            NewThisMonth = users.Count(u => u.CreatedAt >= thisMonthStart),
            GrowthRate = CalculateGrowthRate(users, thisMonthStart)
        };
    }

    private async Task<DriverMetricsDto> GetDriverMetricsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var driverQuery = _context.Set<User>()
            .AsNoTracking()
            .Where(u => u.Role == Role.driver)
            .Include(u => u.DriverQuotes)
            .ThenInclude(dq => dq.Quote)
            .AsQueryable();

        if (fromDate.HasValue)
            driverQuery = driverQuery.Where(u => u.CreatedAt >= fromDate.Value.ToUniversalTime());
        if (toDate.HasValue)
            driverQuery = driverQuery.Where(u => u.CreatedAt <= toDate.Value.ToUniversalTime());

        var drivers = await driverQuery.ToListAsync(ct);

        var totalDrivers = drivers.Count;
        var activeDrivers = drivers.Count(d => d.DriverQuotes.Any());
        var totalAssignments = drivers.Sum(d => d.DriverQuotes.Count);
        var busyDrivers = drivers.Count(d => d.DriverQuotes.Any(dq =>
            dq.Quote.PaymentStatus == PaymentStatus.Pending ||
            dq.Quote.PaymentStatus == PaymentStatus.PartiallyPaid));

        return new DriverMetricsDto
        {
            Total = totalDrivers,
            Active = activeDrivers,
            Available = totalDrivers - busyDrivers,
            Busy = busyDrivers,
            UtilizationRate = totalDrivers > 0 ? (double)activeDrivers / totalDrivers * 100 : 0,
            AverageAssignmentsPerDriver = activeDrivers > 0 ? (double)totalAssignments / activeDrivers : 0
        };
    }

    private async Task<RevenueMetricsDto> GetRevenueMetricsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var query = _context.Set<Quote>()
            .AsNoTracking()
            .Where(q => q.TotalCost.HasValue && q.TotalCost > 0)
            .AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(q => q.CreatedAt >= fromDate.Value.ToUniversalTime());
        if (toDate.HasValue)
            query = query.Where(q => q.CreatedAt <= toDate.Value.ToUniversalTime());

        var quotes = await query.ToListAsync(ct);

        var thisMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var lastMonthStart = thisMonthStart.AddMonths(-1);
        var lastMonthEnd = thisMonthStart.AddDays(-1);

        var thisMonthRevenue = quotes
            .Where(q => q.CreatedAt >= thisMonthStart)
            .Sum(q => q.TotalCost ?? 0);

        var lastMonthRevenue = quotes
            .Where(q => q.CreatedAt >= lastMonthStart && q.CreatedAt <= lastMonthEnd)
            .Sum(q => q.TotalCost ?? 0);

        return new RevenueMetricsDto
        {
            Total = quotes.Sum(q => q.TotalCost ?? 0),
            ThisMonth = thisMonthRevenue,
            LastMonth = lastMonthRevenue,
            MonthOverMonthGrowth = lastMonthRevenue > 0 ?
                (double)(thisMonthRevenue - lastMonthRevenue) / (double)lastMonthRevenue * 100 : 0,
            ByServiceType = new ServiceTypeRevenueDto
            {
                Send = quotes.Where(q => q.Type == QuoteType.Send).Sum(q => q.TotalCost ?? 0),
                Receive = quotes.Where(q => q.Type == QuoteType.Receive).Sum(q => q.TotalCost ?? 0),
                Removals = quotes.Where(q => q.Type == QuoteType.Removals).Sum(q => q.TotalCost ?? 0)
            }
        };
    }

    private async Task<OperationalMetricsDto> GetOperationalMetricsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var query = _context.Set<Quote>().AsNoTracking().AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(q => q.CreatedAt >= fromDate.Value.ToUniversalTime());
        if (toDate.HasValue)
            query = query.Where(q => q.CreatedAt <= toDate.Value.ToUniversalTime());

        var quotes = await query.ToListAsync(ct);
        var thisMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        return new OperationalMetricsDto
        {
            AverageQuoteValue = quotes.Count > 0 ? quotes.Average(q => q.TotalCost ?? 0) : 0,
            QuotesCompletedThisMonth = quotes.Count(q =>
                q.CreatedAt >= thisMonthStart &&
                (q.PaymentStatus == PaymentStatus.Succeeded || q.PaymentStatus == PaymentStatus.Paid)),
            AverageCompletionTime = 2.5, // Mock value - would need actual completion tracking
            CustomerSatisfactionScore = 4.2 // Mock value - would need feedback system
        };
    }

    private static double CalculateGrowthRate(List<User> users, DateTime thisMonthStart)
    {
        var lastMonthStart = thisMonthStart.AddMonths(-1);
        var lastMonthEnd = thisMonthStart.AddDays(-1);

        var thisMonthUsers = users.Count(u => u.CreatedAt >= thisMonthStart);
        var lastMonthUsers = users.Count(u => u.CreatedAt >= lastMonthStart && u.CreatedAt <= lastMonthEnd);

        return lastMonthUsers > 0 ? (double)(thisMonthUsers - lastMonthUsers) / lastMonthUsers * 100 : 0;
    }
}
