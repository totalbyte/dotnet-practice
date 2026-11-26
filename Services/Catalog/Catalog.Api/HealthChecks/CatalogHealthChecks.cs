using Catalog.Bll.Caching;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Platform.ServiceDefaults.HealthChecks;

namespace Catalog.Api.HealthChecks;

public static class CatalogHealthChecks
{
    public static IHealthChecksBuilder AddCatalogHealthChecks(this IServiceCollection services, string connectionString)
        => services.AddHealthChecks()
            .AddSqlServer(
                connectionString,
                healthQuery: "SELECT 1;",
                name: "catalog-db",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["database", "sql", Extensions.ReadyTag],
                timeout: TimeSpan.FromSeconds(5))
            // RedisCacheHealthCheck reports failureStatus (Degraded) both without a connection and without
            // a PING reply: the cache is optional, without it the service reads from the database.
            .AddCheck<RedisCacheHealthCheck>(
                "redis-cache",
                failureStatus: HealthStatus.Degraded,
                tags: ["cache", "redis", Extensions.ReadyTag],
                timeout: TimeSpan.FromSeconds(3))
            .AddCheck<CacheStatisticsHealthCheck>("cache", tags: ["cache"]);
}
