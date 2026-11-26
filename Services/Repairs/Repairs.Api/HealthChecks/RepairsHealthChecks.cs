using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Repairs.Api.HealthChecks;

public static class RepairsHealthChecks
{
    public static IHealthChecksBuilder AddRepairsHealthChecks(this IServiceCollection services, string connectionString)
        => services.AddHealthChecks()
            .AddSqlServer(
                connectionString,
                healthQuery: "SELECT 1;",
                name: "repairs-db",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["database", "sql", Extensions.ReadyTag],
                timeout: TimeSpan.FromSeconds(5));
}
