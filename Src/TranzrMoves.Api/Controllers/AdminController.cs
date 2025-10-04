using Mediator;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

using TranzrMoves.Application.Features.Admin.Dashboard;

namespace TranzrMoves.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
// [Authorize(Roles = "admin")]
public class AdminController(
    IMediator mediator,
    IMemoryCache cache,
    ILogger<AdminController> logger)
    : ControllerBase
{
    private const int _cacheExpirationMinutes = 5;

    /// <summary>
    /// Get dashboard metrics for admin panel
    /// </summary>
    /// <param name="fromDate">Optional start date for filtering metrics</param>
    /// <param name="toDate">Optional end date for filtering metrics</param>
    /// <returns>Dashboard metrics including quotes, payments, users, drivers, revenue, and operational data</returns>
    [HttpGet("dashboard/metrics")]
    [ProducesResponseType(typeof(DashboardMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDashboardMetrics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            // Validate date range
            if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            {
                return BadRequest("From date must be before or equal to to date");
            }

            // Generate cache key based on parameters
            var cacheKey = GenerateCacheKey(fromDate, toDate);

            // Try to get from cache first
            if (cache.TryGetValue(cacheKey, out DashboardMetricsDto? cachedMetrics))
            {
                logger.LogInformation("Dashboard metrics retrieved from cache for key: {CacheKey}", cacheKey);
                return Ok(cachedMetrics);
            }

            // Get from service if not in cache
            logger.LogInformation("Fetching dashboard metrics from service for date range: {FromDate} to {ToDate}",
                fromDate, toDate);

            var query = new GetDashboardMetricsQuery(fromDate, toDate);
            var metricsResult = await mediator.Send(query, HttpContext.RequestAborted);

            if (metricsResult.IsError)
            {
                logger.LogError("Failed to retrieve dashboard metrics: {Error}", metricsResult.FirstError.Description);
                return StatusCode(500, "An error occurred while retrieving dashboard metrics");
            }

            var metrics = metricsResult.Value;

            // Store in cache with expiration
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheExpirationMinutes),
                Priority = CacheItemPriority.Normal
            };

            cache.Set(cacheKey, metrics, cacheEntryOptions);

            logger.LogInformation("Dashboard metrics cached successfully for key: {CacheKey}", cacheKey);

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving dashboard metrics for date range: {FromDate} to {ToDate}",
                fromDate, toDate);

            return StatusCode(500, "An error occurred while retrieving dashboard metrics");
        }
    }

    /// <summary>
    /// Clear dashboard metrics cache
    /// </summary>
    /// <returns>Success message</returns>
    [HttpDelete("dashboard/cache")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult ClearDashboardCache()
    {
        try
        {
            // Clear all dashboard metrics cache entries
            // Note: In a production environment, you might want to use a more sophisticated cache invalidation strategy
            if (cache is MemoryCache memoryCache)
            {
                var field = typeof(MemoryCache).GetField("_coherentState",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var coherentState = field?.GetValue(memoryCache);
                var entriesCollection = coherentState?.GetType().GetProperty("EntriesCollection",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var entries = entriesCollection?.GetValue(coherentState) as System.Collections.IDictionary;

                if (entries != null)
                {
                    var keysToRemove = new List<object>();
                    foreach (System.Collections.DictionaryEntry entry in entries)
                    {
                        if (entry.Key.ToString()?.StartsWith("admin_dashboard_metrics") == true)
                        {
                            keysToRemove.Add(entry.Key);
                        }
                    }

                    foreach (var key in keysToRemove)
                    {
                        cache.Remove(key);
                    }
                }
            }

            logger.LogInformation("Dashboard metrics cache cleared successfully");
            return Ok(new { message = "Dashboard metrics cache cleared successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error clearing dashboard metrics cache");
            return StatusCode(500, "An error occurred while clearing the cache");
        }
    }

    private static string GenerateCacheKey(DateTime? fromDate, DateTime? toDate)
    {
        if (fromDate.HasValue && toDate.HasValue)
        {
            return $"admin_dashboard_metrics_{fromDate.Value:yyyy-MM-dd}_{toDate.Value:yyyy-MM-dd}";
        }

        return "admin_dashboard_metrics";
    }
}
