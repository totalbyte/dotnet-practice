using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Platform.Aggregator.Dtos;
using Platform.Aggregator.Services;

namespace Platform.Aggregator.Caching;

public sealed class RepairOrderCardCache(
    IDistributedCache redis,
    AggregatorCacheMetrics metrics,
    IOptions<AggregatorOptions> options,
    TimeProvider time,
    ILogger<RepairOrderCardCache> logger)
{
    public const string EntryKind = "repair-order-card";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private long _retryAtTicks;

    public bool IsAvailable => time.GetUtcNow().UtcTicks >= Interlocked.Read(ref _retryAtTicks);

    public static string Key(long repairOrderId) => $"aggregator:repair-order:{repairOrderId}:card";

    public async Task<RepairOrderCardDto?> GetAsync(long repairOrderId, CancellationToken ct)
    {
        var key = Key(repairOrderId);
        if (!IsAvailable)
            return null;

        var startedAt = Stopwatch.GetTimestamp();
        try
        {
            var bytes = await redis.GetAsync(key, ct);
            metrics.Operation("get", Stopwatch.GetElapsedTime(startedAt));

            if (bytes is null)
            {
                metrics.Miss(EntryKind);
                logger.LogDebug("Cache miss {CacheKey} in {CacheLevel}", key, AggregatorCacheMetrics.DistributedLevel);
                return null;
            }

            metrics.Hit(EntryKind);
            logger.LogDebug("Cache hit {CacheKey} in {CacheLevel}: {SizeBytes} bytes",
                key, AggregatorCacheMetrics.DistributedLevel, bytes.Length);
            return JsonSerializer.Deserialize<RepairOrderCardDto>(bytes, SerializerOptions);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            MarkUnavailable(ex, "get", key);
            return null;
        }
    }

    public async Task SetAsync(RepairOrderCardDto card, CancellationToken ct)
    {
        var key = Key(card.Order.Id);
        if (card.Warnings.Count > 0)
        {
            metrics.Skipped(EntryKind);
            logger.LogDebug("Partial card {CacheKey} with {WarningsCount} warnings is not cached", key, card.Warnings.Count);
            return;
        }

        if (!IsAvailable)
            return;

        var timeToLive = options.Value.CardCacheTimeToLive;
        var startedAt = Stopwatch.GetTimestamp();
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(card, SerializerOptions);
            await redis.SetAsync(key, bytes,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = timeToLive }, ct);
            metrics.Operation("set", Stopwatch.GetElapsedTime(startedAt));
            logger.LogDebug("Cache entry {CacheKey} stored in {CacheLevel}: {SizeBytes} bytes for {TimeToLive}",
                key, AggregatorCacheMetrics.DistributedLevel, bytes.Length, timeToLive);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            MarkUnavailable(ex, "set", key);
        }
    }

    private void MarkUnavailable(Exception exception, string operation, string key)
    {
        var retryAfter = options.Value.CacheRetryAfter;
        Interlocked.Exchange(ref _retryAtTicks, (time.GetUtcNow() + retryAfter).UtcTicks);
        metrics.Failed(operation);
        logger.LogWarning(exception,
            "Redis failed on {CacheOperation} of {CacheKey}; cards are built from the services without caching for {RetryAfter}",
            operation, key, retryAfter);
    }
}
