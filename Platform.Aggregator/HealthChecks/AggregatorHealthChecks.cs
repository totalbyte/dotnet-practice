using Grpc.Health.V1;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Resilience;
using Platform.ServiceDefaults.HealthChecks;

namespace Platform.Aggregator.HealthChecks;

public static class AggregatorHealthChecks
{
    public static readonly TimeSpan GrpcCheckDeadline = TimeSpan.FromSeconds(2);

    public static IServiceCollection AddAggregatorHealthChecks(this IServiceCollection services)
    {
        var checks = services.AddHealthChecks();

        foreach (var policy in UpstreamPolicies.All)
        {
            var clientName = HealthClientName(policy.Service);

            services.AddGrpcClient<Health.HealthClient>(clientName, options =>
                    options.Address = UpstreamServices.GrpcAddress(policy.Service))
                .RemoveAllResilienceHandlers();

            checks.Add(new HealthCheckRegistration(
                policy.Service,
                serviceProvider => new DownstreamGrpcHealthCheck(
                    serviceProvider.GetRequiredService<GrpcClientFactory>().CreateClient<Health.HealthClient>(clientName),
                    policy.Service,
                    GrpcCheckDeadline,
                    serviceProvider.GetRequiredService<TimeProvider>(),
                    serviceProvider.GetRequiredService<ILogger<DownstreamGrpcHealthCheck>>()),
                policy.IsCritical ? HealthStatus.Unhealthy : HealthStatus.Degraded,
                ["grpc", "downstream", Extensions.ReadyTag],
                GrpcCheckDeadline + TimeSpan.FromSeconds(1)));
        }

        checks.AddCheck<RedisCacheHealthCheck>(
            "redis-cache",
            failureStatus: HealthStatus.Degraded,
            tags: ["cache", "redis", Extensions.ReadyTag],
            timeout: TimeSpan.FromSeconds(3));

        return services;
    }

    public static string HealthClientName(string service) => $"{service}-health";
}
