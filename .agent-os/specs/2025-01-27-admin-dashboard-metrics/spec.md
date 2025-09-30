# Spec Requirements Document

> Spec: Admin Dashboard Metrics API Endpoint
> Created: 2025-01-27
> Priority: High

## Overview

Implement a dedicated API endpoint for admin dashboard metrics that aggregates data from multiple database tables to provide real-time business intelligence for the Tranzr admin application. This endpoint will replace the current client-side aggregation approach with a single, optimized server-side call.

## User Stories

### Admin Dashboard Metrics

As an admin user, I want to see real-time aggregated metrics on the dashboard, so that I can monitor business performance and make data-driven decisions.

**Detailed Workflow:**
- View total quote counts by status (pending, paid, completed, cancelled)
- Monitor revenue metrics and payment success rates
- Track driver availability and utilization rates
- See user growth and registration trends
- Access key performance indicators for business decisions

## Spec Scope

1. **Dashboard Metrics Endpoint** - Create `/api/v1/admin/dashboard/metrics` endpoint
2. **Database Aggregations** - Implement efficient SQL queries for metrics calculation
3. **Caching Strategy** - Add server-side caching for performance optimization
4. **Error Handling** - Robust error handling with fallback mechanisms

## Out of Scope

- Real-time WebSocket updates (future enhancement)
- Historical trend analysis (separate spec)
- Custom date range filtering (future enhancement)
- Export functionality (handled by existing endpoints)

## Expected Deliverable

1. **Functional API Endpoint** - `/api/v1/admin/dashboard/metrics` working with proper aggregations
2. **Performance Optimization** - Response times under 500ms with caching
3. **Frontend Integration** - Admin dashboard successfully consuming real API data
4. **Error Handling** - Graceful degradation and proper error responses

## Technical Requirements

### Database Schema Dependencies

**Primary Tables:**
- `Quotes` - Quote data with payment status and costs
- `Users` - User accounts with roles and creation dates
- `DriverQuotes` - Driver assignments to quotes
- `QuoteAdditionalPayments` - Additional payment transactions

**Key Fields:**
- `Quotes.PaymentStatus` (enum: Pending=0, Paid=1, PartiallyPaid=2, PaymentSetup=3, Failed=4, Succeeded=5, Cancelled=6)
- `Quotes.PaymentType` (enum: Full=0, Deposit=1, Later=2, Balance=3, Adhoc=4)
- `Quotes.QuoteType` (enum: Send=0, Receive=1, Removals=2)
- `Users.Role` (enum: none=0, customer=1, admin=2, driver=3, commercial_client=4)

### Performance Requirements

- **Response Time**: < 500ms for cached responses
- **Cache Duration**: 5 minutes
- **Concurrent Users**: Support 50+ simultaneous admin users
- **Data Freshness**: Maximum 5-minute delay acceptable

### Security Requirements

- **Authentication**: JWT bearer token validation
- **Authorization**: Admin role required
- **Rate Limiting**: 100 requests per minute per user
- **Data Privacy**: No sensitive customer data in metrics

## Implementation Details

### Endpoint Specification

**URL:** `GET /api/v1/admin/dashboard/metrics`

**Headers:**
```
Authorization: Bearer <jwt_token>
Content-Type: application/json
```

**Query Parameters:**
- `fromDate` (optional): ISO 8601 date string for date range start
- `toDate` (optional): ISO 8601 date string for date range end

**Response:**
```json
{
  "quotes": {
    "total": 150,
    "pending": 25,
    "paid": 100,
    "partiallyPaid": 15,
    "succeeded": 8,
    "cancelled": 2,
    "byType": {
      "send": 80,
      "receive": 45,
      "removals": 25
    }
  },
  "payments": {
    "totalAmount": 125000.00,
    "completedAmount": 100000.00,
    "pendingAmount": 25000.00,
    "totalTransactions": 120,
    "successRate": 95.5,
    "averageOrderValue": 1041.67,
    "byPaymentType": {
      "full": 60,
      "deposit": 40,
      "later": 15,
      "balance": 5
    }
  },
  "drivers": {
    "total": 25,
    "active": 20,
    "available": 15,
    "busy": 5,
    "utilizationRate": 25.0,
    "averageAssignmentsPerDriver": 4.8
  },
  "users": {
    "total": 75,
    "customers": 60,
    "drivers": 12,
    "admins": 3,
    "newThisMonth": 10,
    "growthRate": 15.4
  },
  "revenue": {
    "total": 125000.00,
    "thisMonth": 15000.00,
    "lastMonth": 12000.00,
    "monthOverMonthGrowth": 25.0,
    "byServiceType": {
      "send": 80000.00,
      "receive": 30000.00,
      "removals": 15000.00
    }
  },
  "operational": {
    "averageQuoteValue": 833.33,
    "quotesCompletedThisMonth": 45,
    "averageCompletionTime": 2.5,
    "customerSatisfactionScore": 4.2
  },
  "lastUpdated": "2025-01-27T10:30:00Z",
  "cacheExpiry": "2025-01-27T10:35:00Z"
}
```

**Error Responses:**
```json
{
  "error": "Unauthorized",
  "message": "Admin access required",
  "statusCode": 401
}
```

```json
{
  "error": "Internal Server Error",
  "message": "Failed to retrieve dashboard metrics",
  "statusCode": 500
}
```

### EF Core LINQ Queries

**1. Quote Metrics Query:**
```csharp
private async Task<QuoteMetricsDto> GetQuoteMetricsAsync(TranzrMovesDbContext context, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
{
    var query = context.Quotes.AsQueryable();
    
    if (fromDate.HasValue)
        query = query.Where(q => q.CreatedAt >= fromDate.Value);
    if (toDate.HasValue)
        query = query.Where(q => q.CreatedAt <= toDate.Value);

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
```

**2. Payment Metrics Query:**
```csharp
private async Task<PaymentMetricsDto> GetPaymentMetricsAsync(TranzrMovesDbContext context, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
{
    var query = context.Quotes
        .Where(q => q.TotalCost.HasValue && q.TotalCost > 0)
        .AsQueryable();
    
    if (fromDate.HasValue)
        query = query.Where(q => q.CreatedAt >= fromDate.Value);
    if (toDate.HasValue)
        query = query.Where(q => q.CreatedAt <= toDate.Value);

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
```

**3. User Metrics Query:**
```csharp
private async Task<UserMetricsDto> GetUserMetricsAsync(TranzrMovesDbContext context, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
{
    var query = context.Users.AsQueryable();
    
    if (fromDate.HasValue)
        query = query.Where(u => u.CreatedAt >= fromDate.Value);
    if (toDate.HasValue)
        query = query.Where(u => u.CreatedAt <= toDate.Value);

    var users = await query.ToListAsync(ct);
    var thisMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
    
    return new UserMetricsDto
    {
        Total = users.Count,
        Customers = users.Count(u => u.Role == Role.customer),
        Drivers = users.Count(u => u.Role == Role.driver),
        Admins = users.Count(u => u.Role == Role.admin),
        NewThisMonth = users.Count(u => u.CreatedAt >= thisMonthStart),
        GrowthRate = CalculateGrowthRate(users, thisMonthStart) // Helper method
    };
}
```

**4. Driver Metrics Query:**
```csharp
private async Task<DriverMetricsDto> GetDriverMetricsAsync(TranzrMovesDbContext context, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
{
    var driverQuery = context.Users
        .Where(u => u.Role == Role.driver)
        .Include(u => u.DriverQuotes)
        .ThenInclude(dq => dq.Quote)
        .AsQueryable();
    
    if (fromDate.HasValue)
        driverQuery = driverQuery.Where(u => u.CreatedAt >= fromDate.Value);
    if (toDate.HasValue)
        driverQuery = driverQuery.Where(u => u.CreatedAt <= toDate.Value);

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
```

**5. Revenue Metrics Query:**
```csharp
private async Task<RevenueMetricsDto> GetRevenueMetricsAsync(TranzrMovesDbContext context, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
{
    var query = context.Quotes
        .Where(q => q.TotalCost.HasValue && q.TotalCost > 0)
        .AsQueryable();
    
    if (fromDate.HasValue)
        query = query.Where(q => q.CreatedAt >= fromDate.Value);
    if (toDate.HasValue)
        query = query.Where(q => q.CreatedAt <= toDate.Value);

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
            (thisMonthRevenue - lastMonthRevenue) / lastMonthRevenue * 100 : 0,
        ByServiceType = new ServiceTypeRevenueDto
        {
            Send = quotes.Where(q => q.Type == QuoteType.Send).Sum(q => q.TotalCost ?? 0),
            Receive = quotes.Where(q => q.Type == QuoteType.Receive).Sum(q => q.TotalCost ?? 0),
            Removals = quotes.Where(q => q.Type == QuoteType.Removals).Sum(q => q.TotalCost ?? 0)
        }
    };
}
```

### Implementation Strategy

**1. Controller Implementation:**
```csharp
[Route("api/v1/admin")]
[ApiController]
public class AdminController : ApiControllerBase
{
    private readonly IAdminMetricsService _metricsService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AdminController> _logger;

    [HttpGet("dashboard/metrics")]
    public async Task<IActionResult> GetDashboardMetrics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken ct = default)
    {
        try
        {
            var cacheKey = $"dashboard_metrics_{fromDate}_{toDate}";
            
            if (_cache.TryGetValue(cacheKey, out DashboardMetricsDto cachedMetrics))
            {
                return Ok(cachedMetrics);
            }

            var metrics = await _metricsService.GetDashboardMetricsAsync(fromDate, toDate, ct);
            
            _cache.Set(cacheKey, metrics, TimeSpan.FromMinutes(5));
            
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard metrics");
            return StatusCode(500, "Failed to retrieve dashboard metrics");
        }
    }
}
```

**2. Service Implementation:**
```csharp
public interface IAdminMetricsService
{
    Task<DashboardMetricsDto> GetDashboardMetricsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct);
}

public class AdminMetricsService : IAdminMetricsService
{
    private readonly IDbContextFactory<TranzrMovesDbContext> _contextFactory;
    private readonly ILogger<AdminMetricsService> _logger;

    public async Task<DashboardMetricsDto> GetDashboardMetricsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        using var context = await _contextFactory.CreateDbContextAsync(ct);
        
        // Execute all queries in parallel for better performance
        var tasks = new[]
        {
            GetQuoteMetricsAsync(context, fromDate, toDate, ct),
            GetPaymentMetricsAsync(context, fromDate, toDate, ct),
            GetUserMetricsAsync(context, fromDate, toDate, ct),
            GetDriverMetricsAsync(context, fromDate, toDate, ct),
            GetRevenueMetricsAsync(context, fromDate, toDate, ct)
        };

        await Task.WhenAll(tasks);

        // Combine results into DTO
        return new DashboardMetricsDto
        {
            Quotes = await tasks[0],
            Payments = await tasks[1],
            Users = await tasks[2],
            Drivers = await tasks[3],
            Revenue = await tasks[4],
            Operational = await GetOperationalMetricsAsync(context, fromDate, toDate, ct),
            LastUpdated = DateTimeOffset.UtcNow,
            CacheExpiry = DateTimeOffset.UtcNow.AddMinutes(5)
        };
    }

    private async Task<OperationalMetricsDto> GetOperationalMetricsAsync(TranzrMovesDbContext context, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var query = context.Quotes.AsQueryable();
        
        if (fromDate.HasValue)
            query = query.Where(q => q.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(q => q.CreatedAt <= toDate.Value);

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

    private double CalculateGrowthRate(List<User> users, DateTime thisMonthStart)
    {
        var lastMonthStart = thisMonthStart.AddMonths(-1);
        var lastMonthEnd = thisMonthStart.AddDays(-1);
        
        var thisMonthUsers = users.Count(u => u.CreatedAt >= thisMonthStart);
        var lastMonthUsers = users.Count(u => u.CreatedAt >= lastMonthStart && u.CreatedAt <= lastMonthEnd);
        
        return lastMonthUsers > 0 ? (double)(thisMonthUsers - lastMonthUsers) / lastMonthUsers * 100 : 0;
    }
}
```

**3. DTOs:**
```csharp
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

public class UserMetricsDto
{
    public int Total { get; set; }
    public int Customers { get; set; }
    public int Drivers { get; set; }
    public int Admins { get; set; }
    public int NewThisMonth { get; set; }
    public double GrowthRate { get; set; }
}

public class DriverMetricsDto
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Available { get; set; }
    public int Busy { get; set; }
    public double UtilizationRate { get; set; }
    public double AverageAssignmentsPerDriver { get; set; }
}

public class RevenueMetricsDto
{
    public decimal Total { get; set; }
    public decimal ThisMonth { get; set; }
    public decimal LastMonth { get; set; }
    public double MonthOverMonthGrowth { get; set; }
    public ServiceTypeRevenueDto ByServiceType { get; set; } = new();
}

public class OperationalMetricsDto
{
    public decimal AverageQuoteValue { get; set; }
    public int QuotesCompletedThisMonth { get; set; }
    public double AverageCompletionTime { get; set; }
    public double CustomerSatisfactionScore { get; set; }
}

public class QuoteTypeBreakdownDto
{
    public int Send { get; set; }
    public int Receive { get; set; }
    public int Removals { get; set; }
}

public class PaymentTypeBreakdownDto
{
    public int Full { get; set; }
    public int Deposit { get; set; }
    public int Later { get; set; }
    public int Balance { get; set; }
}

public class ServiceTypeRevenueDto
{
    public decimal Send { get; set; }
    public decimal Receive { get; set; }
    public decimal Removals { get; set; }
}
```

### Performance Optimization

**1. EF Core Configuration for Performance:**
```csharp
// In DbContext OnModelCreating method
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Quote indexes for dashboard metrics
    modelBuilder.Entity<Quote>()
        .HasIndex(q => q.PaymentStatus)
        .HasDatabaseName("IX_Quotes_PaymentStatus");
    
    modelBuilder.Entity<Quote>()
        .HasIndex(q => q.QuoteType)
        .HasDatabaseName("IX_Quotes_QuoteType");
    
    modelBuilder.Entity<Quote>()
        .HasIndex(q => q.PaymentType)
        .HasDatabaseName("IX_Quotes_PaymentType");
    
    modelBuilder.Entity<Quote>()
        .HasIndex(q => q.CreatedAt)
        .HasDatabaseName("IX_Quotes_CreatedAt");
    
    modelBuilder.Entity<Quote>()
        .HasIndex(q => q.TotalCost)
        .HasDatabaseName("IX_Quotes_TotalCost")
        .HasFilter("TotalCost IS NOT NULL");
    
    // User indexes
    modelBuilder.Entity<User>()
        .HasIndex(u => u.Role)
        .HasDatabaseName("IX_Users_Role");
    
    modelBuilder.Entity<User>()
        .HasIndex(u => u.CreatedAt)
        .HasDatabaseName("IX_Users_CreatedAt");
    
    // DriverQuote indexes
    modelBuilder.Entity<DriverQuote>()
        .HasIndex(dq => dq.UserId)
        .HasDatabaseName("IX_DriverQuotes_UserId");
    
    modelBuilder.Entity<DriverQuote>()
        .HasIndex(dq => dq.QuoteId)
        .HasDatabaseName("IX_DriverQuotes_QuoteId");
}
```

**2. Caching Strategy:**
- Use `IMemoryCache` for 5-minute cache duration
- Cache key includes date range parameters
- Implement cache invalidation on data changes
- Consider Redis for distributed caching in production

**3. EF Core Query Optimization:**
- Use `AsQueryable()` for dynamic query building
- Execute multiple queries in parallel with `Task.WhenAll()`
- Use `Include()` and `ThenInclude()` for related data loading
- Apply `Where()` clauses early to limit data scope
- Use `ToListAsync()` for async operations
- Consider `AsNoTracking()` for read-only queries
- Use `Count()`, `Sum()`, `Average()` LINQ methods for aggregations

### Error Handling

**1. Database Errors:**
- Log all database exceptions
- Return partial data when possible
- Implement circuit breaker pattern for database failures

**2. Cache Errors:**
- Gracefully handle cache failures
- Fall back to database queries
- Log cache-related errors

**3. Validation:**
- Validate date range parameters
- Ensure fromDate <= toDate
- Limit date range to reasonable bounds (e.g., 1 year)

### Testing Requirements

**1. Unit Tests:**
- Test all metric calculations
- Test error handling scenarios
- Test caching behavior

**2. Integration Tests:**
- Test endpoint with real database
- Test performance under load
- Test cache invalidation

**3. Performance Tests:**
- Ensure response times under 500ms
- Test concurrent user scenarios
- Monitor memory usage

### Deployment Considerations

**1. Environment Variables:**
- Cache duration configuration
- Database connection settings
- Logging levels

**2. Monitoring:**
- Add application insights
- Monitor query performance
- Track cache hit rates

**3. Rollback Strategy:**
- Feature flag for new endpoint
- Fallback to existing client-side aggregation
- Database migration rollback plan

## Success Criteria

1. **Functional Requirements:**
   - ✅ Endpoint returns all required metrics
   - ✅ Response time under 500ms
   - ✅ Proper error handling and logging
   - ✅ Caching working correctly

2. **Performance Requirements:**
   - ✅ Supports 50+ concurrent users
   - ✅ Cache hit rate > 80%
   - ✅ Database queries optimized

3. **Integration Requirements:**
   - ✅ Frontend successfully consumes endpoint
   - ✅ No breaking changes to existing functionality
   - ✅ Proper authentication and authorization

## Future Enhancements

1. **Real-time Updates:** WebSocket integration for live metrics
2. **Historical Trends:** Time-series data for trend analysis
3. **Custom Dashboards:** User-configurable metric displays
4. **Export Functionality:** CSV/PDF export of metrics
5. **Advanced Filtering:** More granular date range and filter options

## Dependencies

- Existing database schema and entities
- Authentication and authorization middleware
- Caching infrastructure (IMemoryCache or Redis)
- Logging framework
- Performance monitoring tools

## Risks and Mitigation

**Risk:** Database performance impact from complex queries
**Mitigation:** Implement proper indexing, caching, and query optimization

**Risk:** Cache invalidation complexity
**Mitigation:** Use simple time-based expiration, implement cache warming

**Risk:** High memory usage from caching
**Mitigation:** Monitor memory usage, implement cache size limits

**Risk:** Data consistency issues
**Mitigation:** Use appropriate isolation levels, implement data validation
