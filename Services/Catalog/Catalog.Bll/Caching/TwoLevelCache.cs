using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Catalog.Bll.Caching;

public sealed class TwoLevelCache(
    IMemoryCache memory,
    IDistributedCache distributed,
    CatalogCacheMetrics metrics,
    IOptions<CatalogCacheOptions> options,
    TimeProvider time,
    ILogger<TwoLevelCache> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ConcurrentDictionary<string, string> _pendingRemovals = new();
    private long _distributedRetryAtTicks;

    public bool IsDistributedAvailable => time.GetUtcNow().UtcTicks >= Interlocked.Read(ref _distributedRetryAtTicks);

    public int PendingInvalidations => _pendingRemovals.Count;

    public async Task<T> GetOrCreateAsync<T>(
        string key, CacheEntryPolicy policy, Func<CancellationToken, Task<T>> factory, CancellationToken ct = default)
        where T : class
    {
        if (TryGetFromMemory(key, out T? value))
            return value;

        if (await GetFromDistributedAsync<T>(key, ct) is { } cached)
        {
            SetMemory(key, cached.Value, cached.SizeBytes, policy);
            return cached.Value;
        }

        value = await factory(ct);
        await StoreAsync(key, value, policy, ct);
        return value;
    }

    public async Task<IReadOnlyList<T>> GetOrCreateManyAsync<TId, T>(
        IReadOnlyCollection<TId> ids,
        Func<TId, string> keyOf,
        Func<T, TId> idOf,
        CacheEntryPolicy policy,
        Func<IReadOnlyCollection<TId>, CancellationToken, Task<IReadOnlyList<T>>> factory,
        CancellationToken ct = default)
        where TId : notnull
        where T : class
    {
        var found = new Dictionary<TId, T>();
        var missing = new List<TId>();

        foreach (var id in ids.Distinct())
        {
            if (TryGetFromMemory(keyOf(id), out T? value))
                found[id] = value;
            else
                missing.Add(id);
        }

        var fromRedis = await Task.WhenAll(
            missing.Select(async id => (Id: id, Entry: await GetFromDistributedAsync<T>(keyOf(id), ct))));

        var fromDatabase = new List<TId>();
        foreach (var (id, entry) in fromRedis)
        {
            if (entry is not null)
            {
                SetMemory(keyOf(id), entry.Value, entry.SizeBytes, policy);
                found[id] = entry.Value;
            }
            else
            {
                fromDatabase.Add(id);
            }
        }

        if (fromDatabase.Count > 0)
        {
            foreach (var value in await factory(fromDatabase, ct))
            {
                found[idOf(value)] = value;
                await StoreAsync(keyOf(idOf(value)), value, policy, ct);
            }
        }

        return [.. ids.Distinct().Where(found.ContainsKey).Select(id => found[id])];
    }

    public Task<string> GetVersionAsync(string versionKey, CacheEntryPolicy policy, CancellationToken ct = default)
        => GetOrCreateAsync(versionKey, policy, _ => Task.FromResult(NewVersion()), ct);

    public async Task ChangeVersionAsync(
        string versionKey, CacheEntryPolicy policy, string reason, CancellationToken ct = default)
    {
        if (!await StoreAsync(versionKey, NewVersion(), policy, ct))
            _pendingRemovals[versionKey] = reason;

        metrics.Invalidated(CatalogCacheKeys.EntryKind(versionKey), reason);
        logger.LogInformation("Cache list version {CacheKey} changed, reason {Reason}", versionKey, reason);
    }

    public async Task RemoveAsync(string key, string reason, CancellationToken ct = default)
    {
        if (!await TryDistributedAsync("remove", key, token => distributed.RemoveAsync(key, token), ct))
            _pendingRemovals[key] = reason;

        memory.Remove(key);
        metrics.Invalidated(CatalogCacheKeys.EntryKind(key), reason);
        logger.LogInformation("Cache invalidated {CacheKey}, reason {Reason}", key, reason);
    }

    private bool TryGetFromMemory<T>(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T? value)
        where T : class
    {
        var kind = CatalogCacheKeys.EntryKind(key);
        if (memory.TryGetValue(key, out MemoryEntry? entry) && entry?.Value is T cached)
        {
            metrics.Hit(CatalogCacheMetrics.MemoryLevel, kind);
            logger.LogDebug("Cache hit {CacheKey} in {CacheLevel}: {SizeBytes} bytes, expires in at most {TimeToLive}",
                key, CatalogCacheMetrics.MemoryLevel, entry.SizeBytes, entry.ExpiresAt - time.GetUtcNow());
            value = cached;
            return true;
        }

        metrics.Miss(CatalogCacheMetrics.MemoryLevel, kind);
        logger.LogDebug("Cache miss {CacheKey} in {CacheLevel}", key, CatalogCacheMetrics.MemoryLevel);
        value = null;
        return false;
    }

    private async Task<DistributedEntry<T>?> GetFromDistributedAsync<T>(string key, CancellationToken ct)
        where T : class
    {
        byte[]? bytes = null;
        if (!await TryDistributedAsync("get", key, async token => bytes = await distributed.GetAsync(key, token), ct))
            return null;

        var kind = CatalogCacheKeys.EntryKind(key);
        if (bytes is null)
        {
            metrics.Miss(CatalogCacheMetrics.DistributedLevel, kind);
            logger.LogDebug("Cache miss {CacheKey} in {CacheLevel}", key, CatalogCacheMetrics.DistributedLevel);
            return null;
        }

        try
        {
            var value = JsonSerializer.Deserialize<T>(bytes, SerializerOptions);
            if (value is null)
                return null;

            metrics.Hit(CatalogCacheMetrics.DistributedLevel, kind);
            logger.LogDebug("Cache hit {CacheKey} in {CacheLevel}: {SizeBytes} bytes",
                key, CatalogCacheMetrics.DistributedLevel, bytes.Length);
            return new DistributedEntry<T>(value, bytes.Length);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Cache entry {CacheKey} in {CacheLevel} cannot be read and is ignored",
                key, CatalogCacheMetrics.DistributedLevel);
            metrics.Miss(CatalogCacheMetrics.DistributedLevel, kind);
            return null;
        }
    }

    private async Task<bool> StoreAsync<T>(string key, T value, CacheEntryPolicy policy, CancellationToken ct)
        where T : class
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);
        var entryOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = policy.DistributedExpiration
        };

        var stored = await TryDistributedAsync("set", key,
            token => distributed.SetAsync(key, bytes, entryOptions, token), ct);
        SetMemory(key, value, bytes.Length, policy);

        logger.LogDebug(
            "Cache entry {CacheKey} stored: {SizeBytes} bytes, L1 sliding {MemorySliding} / absolute {MemoryAbsolute}, L2 {DistributedTtl}",
            key, bytes.Length, policy.MemorySlidingExpiration, policy.MemoryAbsoluteExpiration,
            stored ? policy.DistributedExpiration : TimeSpan.Zero);
        return stored;
    }

    private void SetMemory<T>(string key, T value, long sizeBytes, CacheEntryPolicy policy)
        where T : class
    {
        var entryOptions = new MemoryCacheEntryOptions
        {
            Size = Math.Max(1, (sizeBytes + 1023) / 1024),
            SlidingExpiration = policy.MemorySlidingExpiration,
            AbsoluteExpirationRelativeToNow = policy.MemoryAbsoluteExpiration,
            Priority = policy.Priority
        };
        entryOptions.RegisterPostEvictionCallback(static (_, _, reason, state) =>
        {
            if (reason is EvictionReason.Expired or EvictionReason.Capacity or EvictionReason.TokenExpired)
                ((CatalogCacheMetrics)state!).Evicted(reason);
        }, metrics);

        memory.Set(key, new MemoryEntry(value, sizeBytes, time.GetUtcNow() + policy.MemoryAbsoluteExpiration),
            entryOptions);
    }

    private async Task<bool> TryDistributedAsync(
        string operation, string key, Func<CancellationToken, Task> action, CancellationToken ct)
    {
        if (!IsDistributedAvailable || !await FlushPendingRemovalsAsync(ct))
            return false;

        var startedAt = Stopwatch.GetTimestamp();
        try
        {
            await action(ct);
            metrics.DistributedOperation(operation, Stopwatch.GetElapsedTime(startedAt));
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            MarkDistributedUnavailable(ex, operation, key);
            return false;
        }
    }

    private async Task<bool> FlushPendingRemovalsAsync(CancellationToken ct)
    {
        foreach (var (key, reason) in _pendingRemovals)
        {
            try
            {
                await distributed.RemoveAsync(key, ct);
                _pendingRemovals.TryRemove(key, out _);
                logger.LogInformation("Deferred cache invalidation of {CacheKey} applied in {CacheLevel}, reason {Reason}",
                    key, CatalogCacheMetrics.DistributedLevel, reason);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                MarkDistributedUnavailable(ex, "remove", key);
                return false;
            }
        }

        return true;
    }

    private void MarkDistributedUnavailable(Exception exception, string operation, string key)
    {
        var retryAfter = options.Value.DistributedRetryAfter;
        Interlocked.Exchange(ref _distributedRetryAtTicks, (time.GetUtcNow() + retryAfter).UtcTicks);
        metrics.Failed(operation);
        logger.LogWarning(exception,
            "Distributed cache {CacheLevel} failed on {CacheOperation} of {CacheKey}; serving from L1 and the database for {RetryAfter}",
            CatalogCacheMetrics.DistributedLevel, operation, key, retryAfter);
    }

    private static string NewVersion() => Guid.NewGuid().ToString("N")[..8];

    private sealed record MemoryEntry(object Value, long SizeBytes, DateTimeOffset ExpiresAt);

    private sealed record DistributedEntry<T>(T Value, int SizeBytes);
}
