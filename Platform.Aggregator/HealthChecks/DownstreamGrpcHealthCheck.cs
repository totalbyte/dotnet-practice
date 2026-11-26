using Grpc.Core;
using Grpc.Health.V1;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Platform.Aggregator.HealthChecks;

public sealed class DownstreamGrpcHealthCheck(
    Health.HealthClient client,
    string service,
    TimeSpan deadline,
    TimeProvider time,
    ILogger<DownstreamGrpcHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var reply = await client.CheckAsync(new HealthCheckRequest(),
                deadline: time.GetUtcNow().UtcDateTime.Add(deadline), cancellationToken: ct);

            return reply.Status == HealthCheckResponse.Types.ServingStatus.Serving
                ? HealthCheckResult.Healthy($"{service} is serving")
                : new HealthCheckResult(context.Registration.FailureStatus, $"{service} reports {reply.Status}");
        }
        catch (RpcException ex)
        {
            logger.LogWarning(ex, "Health check of {Service} failed with {GrpcStatus}", service, ex.StatusCode);
            return new HealthCheckResult(context.Registration.FailureStatus, $"{service} is unavailable", ex);
        }
    }
}
