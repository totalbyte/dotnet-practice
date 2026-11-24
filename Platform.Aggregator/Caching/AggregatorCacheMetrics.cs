using System.Diagnostics.Metrics;

namespace Platform.Aggregator.Caching;

public sealed class AggregatorCacheMetrics
{
    public const string MeterName = "Platform.Cache";
    public const string DistributedLevel = "L2";

    private readonly Counter<long> _hits;
    private readonly Counter<long> _misses;
    private readonly Counter<long> _skipped;
    private readonly Counter<long> _errors;
    private readonly Histogram<double> _duration;

    public AggregatorCacheMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _hits = meter.CreateCounter<long>("cache.hits", "{hit}", "Aggregated cards served from Redis");
        _misses = meter.CreateCounter<long>("cache.misses", "{miss}", "Aggregated cards built from the services");
        _skipped = meter.CreateCounter<long>(
            "cache.skipped", "{entry}", "Partial cards that were not cached because a service was unavailable");
        _errors = meter.CreateCounter<long>("cache.errors", "{error}", "Failed Redis operations");
        _duration = meter.CreateHistogram<double>("cache.operation.duration", "ms", "Duration of Redis operations");
    }

    public void Hit(string entry) => _hits.Add(1, Level, Entry(entry));

    public void Miss(string entry) => _misses.Add(1, Level, Entry(entry));

    public void Skipped(string entry) => _skipped.Add(1, Level, Entry(entry));

    public void Failed(string operation) => _errors.Add(1, Level, Operation(operation));

    public void Operation(string operation, TimeSpan elapsed)
        => _duration.Record(elapsed.TotalMilliseconds, Level, Operation(operation));

    private static KeyValuePair<string, object?> Level => new("cache.level", DistributedLevel);

    private static KeyValuePair<string, object?> Entry(string entry) => new("cache.entry", entry);

    private static KeyValuePair<string, object?> Operation(string operation) => new("cache.operation", operation);
}
