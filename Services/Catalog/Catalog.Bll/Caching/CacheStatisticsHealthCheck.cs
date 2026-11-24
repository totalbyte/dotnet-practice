using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Catalog.Bll.Caching;

public sealed class CacheStatisticsHealthCheck(IMemoryCache memory, TwoLevelCache cache) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var statistics = memory.GetCurrentStatistics();
        var lookups = (statistics?.TotalHits ?? 0) + (statistics?.TotalMisses ?? 0);
        var data = new Dictionary<string, object>
        {
            ["l1.entries"] = statistics?.CurrentEntryCount ?? 0,
            ["l1.sizeKilobytes"] = statistics?.CurrentEstimatedSize ?? 0,
            ["l1.hits"] = statistics?.TotalHits ?? 0,
            ["l1.misses"] = statistics?.TotalMisses ?? 0,
            ["l1.hitRatio"] = lookups == 0 ? 0 : Math.Round((double)statistics!.TotalHits / lookups, 3),
            ["l2.available"] = cache.IsDistributedAvailable,
            ["l2.pendingInvalidations"] = cache.PendingInvalidations
        };

        return Task.FromResult(cache.IsDistributedAvailable
            ? HealthCheckResult.Healthy("Two-level cache is serving from L1 and L2", data)
            : HealthCheckResult.Degraded("L2 (Redis) is unavailable, cache is serving from L1 and the database", data: data));
    }
}
