using System.Text;
using Catalog.Bll.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Catalog.Tests;

public class TwoLevelCacheTests
{
    private const string Key = "catalog:item:1";

    private readonly MemoryCache _memory = new(new MemoryCacheOptions { SizeLimit = 1024, TrackStatistics = true });
    private readonly IDistributedCache _redis =
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
    private readonly FakeTimeProvider _time = new();
    private readonly CatalogCacheOptions _options = new();

    private TwoLevelCache CreateCache(IDistributedCache? distributed = null) => new(
        _memory, distributed ?? _redis, new CatalogCacheMetrics(new TestMeterFactory(), _memory),
        Options.Create(_options), _time, NullLogger<TwoLevelCache>.Instance);

    [Fact]
    public async Task GetOrCreateAsync_Miss_LoadsOnceAndFillsBothLevels()
    {
        var cache = CreateCache();
        var calls = 0;

        var first = await cache.GetOrCreateAsync(Key, _options.Items, _ => Task.FromResult(Load(ref calls)));
        var second = await cache.GetOrCreateAsync(Key, _options.Items, _ => Task.FromResult(Load(ref calls)));

        Assert.Equal(1, calls);
        Assert.Same(first, second);
        Assert.NotNull(await _redis.GetAsync(Key));
    }

    [Fact]
    public async Task GetOrCreateAsync_OnlyL2HasEntry_ReturnsItWithoutLoadingAndFillsL1()
    {
        await _redis.SetAsync(Key, Encoding.UTF8.GetBytes("""{"id":1,"name":"From Redis"}"""));
        var cache = CreateCache();

        var value = await cache.GetOrCreateAsync<Item>(Key, _options.Items,
            _ => throw new InvalidOperationException("database must not be called"));

        Assert.Equal("From Redis", value.Name);
        Assert.True(_memory.TryGetValue(Key, out _));
    }

    [Fact]
    public async Task GetOrCreateAsync_RedisFails_FallsBackToDatabaseAndSkipsRedisUntilRetry()
    {
        var failing = Substitute.For<IDistributedCache>();
        failing.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(new TimeoutException("redis down"));
        var cache = CreateCache(failing);
        var calls = 0;

        var value = await cache.GetOrCreateAsync(Key, _options.Items, _ => Task.FromResult(Load(ref calls)));
        _memory.Remove(Key);
        await cache.GetOrCreateAsync(Key, _options.Items, _ => Task.FromResult(Load(ref calls)));

        Assert.Equal(1, value.Id);
        Assert.Equal(2, calls);
        Assert.False(cache.IsDistributedAvailable);
        await failing.Received(1).GetAsync(Key, Arg.Any<CancellationToken>());

        _time.Advance(_options.DistributedRetryAfter);
        Assert.True(cache.IsDistributedAvailable);
    }

    [Fact]
    public async Task RemoveAsync_RedisFails_RemovesFromL1AndAppliesRemovalWhenRedisRecovers()
    {
        var flaky = Substitute.For<IDistributedCache>();
        flaky.RemoveAsync(Key, Arg.Any<CancellationToken>())
            .Returns(_ => throw new TimeoutException("redis down"), _ => Task.CompletedTask);
        var cache = CreateCache(flaky);
        _memory.Set(Key, "stale", new MemoryCacheEntryOptions { Size = 1 });

        await cache.RemoveAsync(Key, "CatalogItemUpdated");

        Assert.False(_memory.TryGetValue(Key, out _));
        Assert.Equal(1, cache.PendingInvalidations);

        _time.Advance(_options.DistributedRetryAfter);
        await cache.GetOrCreateAsync("catalog:item:2", _options.Items, _ => Task.FromResult(new Item(2, "Other")));

        Assert.Equal(0, cache.PendingInvalidations);
        await flaky.Received(2).RemoveAsync(Key, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOrCreateManyAsync_LoadsOnlyIdsMissingInBothLevels()
    {
        var cache = CreateCache();
        await cache.GetOrCreateAsync(CatalogCacheKeys.Item(1), _options.Items, _ => Task.FromResult(new Item(1, "One")));
        await _redis.SetAsync(CatalogCacheKeys.Item(2), Encoding.UTF8.GetBytes("""{"id":2,"name":"Two"}"""));
        IReadOnlyCollection<long>? requested = null;

        var items = await cache.GetOrCreateManyAsync<long, Item>([1, 2, 3], CatalogCacheKeys.Item, i => i.Id,
            _options.Items, (ids, _) =>
            {
                requested = ids;
                return Task.FromResult<IReadOnlyList<Item>>([new Item(3, "Three")]);
            });

        Assert.Equal([3L], requested);
        Assert.Equal(["One", "Two", "Three"], items.Select(i => i.Name));
    }

    [Fact]
    public async Task ChangeVersionAsync_ProducesNewListVersion()
    {
        var cache = CreateCache();
        var before = await cache.GetVersionAsync(CatalogCacheKeys.ItemListsVersion, _options.ListVersions);

        await cache.ChangeVersionAsync(CatalogCacheKeys.ItemListsVersion, _options.ListVersions, "CatalogItemCreated");
        var after = await cache.GetVersionAsync(CatalogCacheKeys.ItemListsVersion, _options.ListVersions);

        Assert.NotEqual(before, after);
    }

    [Fact]
    public void DefaultOptions_KeepL1ShorterThanL2()
        => Assert.Empty(new CatalogCacheOptions().Validate());

    private static Item Load(ref int calls)
    {
        calls++;
        return new Item(1, "From database");
    }

    private sealed record Item(long Id, string Name);
}
