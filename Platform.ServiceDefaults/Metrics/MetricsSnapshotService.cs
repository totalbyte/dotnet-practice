using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;

namespace Platform.ServiceDefaults.Metrics;

public abstract class MetricsSnapshotService(IServiceScopeFactory scopes, ILogger logger) : BackgroundService
{
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(30);

    protected virtual TimeSpan Interval => DefaultInterval;

    protected abstract Task RefreshAsync(IServiceProvider services, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            await RefreshOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task RefreshOnceAsync(CancellationToken ct)
    {
        try
        {
            using var suppressed = SuppressInstrumentationScope.Begin();
            await using var scope = scopes.CreateAsyncScope();
            await RefreshAsync(scope.ServiceProvider, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Metrics snapshot {Snapshot} failed, the gauge keeps the previous values",
                GetType().Name);
        }
    }
}
