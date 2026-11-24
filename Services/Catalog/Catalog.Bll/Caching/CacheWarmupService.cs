using System.Diagnostics;
using Catalog.Bll.Dtos.Brands;
using Catalog.Bll.Dtos.CatalogItems;
using Catalog.Bll.Dtos.Categories;
using Catalog.Bll.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Catalog.Bll.Caching;

public sealed class CacheWarmupService(
    IServiceScopeFactory scopeFactory,
    IOptions<CatalogCacheOptions> options,
    TimeProvider time,
    ILogger<CacheWarmupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var warmup = options.Value.Warmup;
        if (!warmup.Enabled)
            return;

        using var timer = new PeriodicTimer(warmup.Interval, time);
        do
        {
            await WarmUpAsync(warmup.ItemCount, stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task WarmUpAsync(int itemCount, CancellationToken ct)
    {
        var startedAt = Stopwatch.GetTimestamp();
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var categories = scope.ServiceProvider.GetRequiredService<ICategoryService>();
            var brands = scope.ServiceProvider.GetRequiredService<IBrandService>();
            var items = scope.ServiceProvider.GetRequiredService<ICatalogItemService>();

            var categoryPage = await categories.GetListAsync(new CategoryQuery(), ct);
            var brandPage = await brands.GetListAsync(new BrandQuery(), ct);
            var itemPage = await items.GetListAsync(new CatalogItemQuery { PageSize = itemCount }, ct);
            var cards = await items.GetByIdsAsync([.. itemPage.Items.Select(item => item.Id)], ct);

            logger.LogInformation(
                "Cache warm-up loaded {CategoryCount} categories, {BrandCount} brands and {ItemCount} catalog items in {ElapsedMs} ms",
                categoryPage.Items.Count, brandPage.Items.Count, cards.Count,
                (long)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Cache warm-up failed after {ElapsedMs} ms; entries will be loaded on demand",
                (long)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
        }
    }
}
