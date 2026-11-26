namespace Catalog.Api.Metrics;

public static class CatalogMetricsRegistration
{
    public static IServiceCollection AddCatalogMetrics(this IServiceCollection services)
        => services
            .AddSingleton<CatalogMetrics>()
            .AddHostedService<ActiveCatalogItemsSnapshot>();
}
