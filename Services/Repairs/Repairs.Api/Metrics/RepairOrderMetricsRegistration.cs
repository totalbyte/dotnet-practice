namespace Repairs.Api.Metrics;

public static class RepairOrderMetricsRegistration
{
    public static IServiceCollection AddRepairOrderMetrics(this IServiceCollection services)
        => services
            .AddSingleton<RepairOrderMetrics>()
            .AddHostedService<OpenRepairOrdersSnapshot>();
}
