using Catalog.Bll.Interfaces;
using Catalog.Bll.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Catalog.Bll.Caching;

public static class CachingServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogServicesWithCaching(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CatalogCacheOptions>()
            .Bind(configuration.GetSection(CatalogCacheOptions.SectionName))
            .Validate(o => !o.Validate().Any(), "Caching settings are inconsistent: L1 must expire before L2")
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<CatalogCacheMetrics>();
        services.AddSingleton<TwoLevelCache>();
        services.AddScoped<CatalogCacheInvalidator>();

        services.AddScoped<CatalogItemService>();
        services.AddScoped<CategoryService>();
        services.AddScoped<BrandService>();

        services.AddScoped<ICatalogItemService>(sp => new CachedCatalogItemService(
            sp.GetRequiredService<CatalogItemService>(), sp.GetRequiredService<TwoLevelCache>(),
            sp.GetRequiredService<CatalogCacheInvalidator>(), sp.GetRequiredService<IOptions<CatalogCacheOptions>>()));
        services.AddScoped<ICategoryService>(sp => new CachedCategoryService(
            sp.GetRequiredService<CategoryService>(), sp.GetRequiredService<TwoLevelCache>(),
            sp.GetRequiredService<CatalogCacheInvalidator>(), sp.GetRequiredService<IOptions<CatalogCacheOptions>>()));
        services.AddScoped<IBrandService>(sp => new CachedBrandService(
            sp.GetRequiredService<BrandService>(), sp.GetRequiredService<TwoLevelCache>(),
            sp.GetRequiredService<CatalogCacheInvalidator>(), sp.GetRequiredService<IOptions<CatalogCacheOptions>>()));

        services.AddHostedService<CacheWarmupService>();

        return services;
    }
}
