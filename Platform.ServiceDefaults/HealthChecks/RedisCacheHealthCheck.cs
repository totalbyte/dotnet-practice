using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Platform.ServiceDefaults.HealthChecks;

public sealed class RedisCacheHealthCheck(IConnectionMultiplexer redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        if (!redis.IsConnected)
            return new HealthCheckResult(context.Registration.FailureStatus,
                "Redis is not connected, the service works without the distributed cache");

        try
        {
            var latency = await redis.GetDatabase().PingAsync().WaitAsync(ct);
            return HealthCheckResult.Healthy("Redis answered PING", new Dictionary<string, object>
            {
                ["latencyMs"] = Math.Round(latency.TotalMilliseconds, 1)
            });
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException or OperationCanceledException)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Redis did not answer PING", ex);
        }
    }
}
