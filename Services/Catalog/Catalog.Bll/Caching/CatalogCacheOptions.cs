using Microsoft.Extensions.Caching.Memory;

namespace Catalog.Bll.Caching;

public sealed class CatalogCacheOptions
{
    public const string SectionName = "Caching";

    public long MemorySizeLimitKilobytes { get; set; } = 32 * 1024;

    public CacheEntryPolicy Items { get; set; } = new()
    {
        MemorySlidingExpiration = TimeSpan.FromSeconds(30),
        MemoryAbsoluteExpiration = TimeSpan.FromMinutes(2),
        DistributedExpiration = TimeSpan.FromMinutes(10),
        Priority = CacheItemPriority.Normal
    };

    public CacheEntryPolicy Lists { get; set; } = new()
    {
        MemorySlidingExpiration = TimeSpan.FromSeconds(20),
        MemoryAbsoluteExpiration = TimeSpan.FromMinutes(1),
        DistributedExpiration = TimeSpan.FromMinutes(5),
        Priority = CacheItemPriority.Low
    };

    public CacheEntryPolicy ReferenceData { get; set; } = new()
    {
        MemorySlidingExpiration = TimeSpan.FromMinutes(2),
        MemoryAbsoluteExpiration = TimeSpan.FromMinutes(10),
        DistributedExpiration = TimeSpan.FromHours(1),
        Priority = CacheItemPriority.High
    };

    public CacheEntryPolicy ListVersions { get; set; } = new()
    {
        MemorySlidingExpiration = TimeSpan.FromSeconds(20),
        MemoryAbsoluteExpiration = TimeSpan.FromMinutes(1),
        DistributedExpiration = TimeSpan.FromDays(1),
        Priority = CacheItemPriority.High
    };

    public TimeSpan DistributedRetryAfter { get; set; } = TimeSpan.FromSeconds(30);

    public CacheWarmupOptions Warmup { get; set; } = new();

    public IEnumerable<string> Validate()
    {
        foreach (var (name, policy) in new[]
                 {
                     (nameof(Items), Items), (nameof(Lists), Lists),
                     (nameof(ReferenceData), ReferenceData), (nameof(ListVersions), ListVersions)
                 })
        {
            if (policy.MemorySlidingExpiration > policy.MemoryAbsoluteExpiration)
                yield return $"{name}: memory sliding expiration exceeds memory absolute expiration";
            if (policy.MemoryAbsoluteExpiration >= policy.DistributedExpiration)
                yield return $"{name}: memory (L1) expiration must be shorter than distributed (L2) expiration";
        }

        if (MemorySizeLimitKilobytes <= 0)
            yield return $"{nameof(MemorySizeLimitKilobytes)} must be positive";
    }
}

public sealed class CacheEntryPolicy
{
    public TimeSpan MemorySlidingExpiration { get; set; }
    public TimeSpan MemoryAbsoluteExpiration { get; set; }
    public TimeSpan DistributedExpiration { get; set; }
    public CacheItemPriority Priority { get; set; } = CacheItemPriority.Normal;
}

public sealed class CacheWarmupOptions
{
    public bool Enabled { get; set; } = true;
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(5);
    public int ItemCount { get; set; } = 20;
}
