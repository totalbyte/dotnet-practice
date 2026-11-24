using System.Diagnostics.Metrics;
using Microsoft.Extensions.Caching.Memory;

namespace Catalog.Bll.Caching;

public sealed class CatalogCacheMetrics
{
    public const string MeterName = "Catalog.Cache";
    public const string MemoryLevel = "L1";
    public const string DistributedLevel = "L2";

    private readonly Counter<long> _hits;
    private readonly Counter<long> _misses;
    private readonly Counter<long> _invalidations;
    private readonly Counter<long> _evictions;
    private readonly Counter<long> _errors;
    private readonly Histogram<double> _distributedDuration;

    public CatalogCacheMetrics(IMeterFactory meterFactory, IMemoryCache memory)
    {
        var meter = meterFactory.Create(MeterName);

        _hits = meter.CreateCounter<long>("cache.hits", "{hit}", "Cache lookups answered by the cache level");
        _misses = meter.CreateCounter<long>("cache.misses", "{miss}", "Cache lookups not found in the cache level");
        _invalidations = meter.CreateCounter<long>(
            "cache.invalidations", "{entry}", "Entries removed from L1 and L2 after a write");
        _evictions = meter.CreateCounter<long>(
            "cache.evictions", "{entry}", "L1 entries evicted by expiration or by the size limit");
        _errors = meter.CreateCounter<long>(
            "cache.errors", "{error}", "Failed L2 operations served without the distributed cache");
        _distributedDuration = meter.CreateHistogram<double>(
            "cache.operation.duration", "ms", "Duration of L2 (Redis) operations");

        meter.CreateObservableGauge("cache.memory.entries",
            () => memory.GetCurrentStatistics()?.CurrentEntryCount ?? 0, "{entry}", "Entries held in L1");
        meter.CreateObservableGauge("cache.memory.size",
            () => memory.GetCurrentStatistics()?.CurrentEstimatedSize ?? 0, "KiBy", "Estimated L1 size in kilobytes");
    }

    public void Hit(string level, string kind) => _hits.Add(1, Level(level), Kind(kind));

    public void Miss(string level, string kind) => _misses.Add(1, Level(level), Kind(kind));

    public void Invalidated(string kind, string reason)
        => _invalidations.Add(1, Kind(kind), new KeyValuePair<string, object?>("cache.reason", reason));

    public void Evicted(EvictionReason reason)
        => _evictions.Add(1, Level(MemoryLevel), new KeyValuePair<string, object?>("cache.reason", reason.ToString()));

    public void Failed(string operation) => _errors.Add(1, Level(DistributedLevel), Operation(operation));

    public void DistributedOperation(string operation, TimeSpan elapsed)
        => _distributedDuration.Record(elapsed.TotalMilliseconds, Level(DistributedLevel), Operation(operation));

    private static KeyValuePair<string, object?> Level(string level) => new("cache.level", level);

    private static KeyValuePair<string, object?> Kind(string kind) => new("cache.entry", kind);

    private static KeyValuePair<string, object?> Operation(string operation) => new("cache.operation", operation);
}
