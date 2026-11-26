using Grpc.Core;
using Grpc.Health.V1;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.Aggregator.HealthChecks;

namespace Platform.Tests.Aggregator;

public sealed class DownstreamGrpcHealthCheckTests
{
    [Fact]
    public async Task Serving_service_is_healthy_and_the_call_has_a_short_deadline()
    {
        var client = new FakeHealthClient(() => GrpcCalls.Success(new HealthCheckResponse
        {
            Status = HealthCheckResponse.Types.ServingStatus.Serving
        }));
        var startedAt = DateTime.UtcNow;

        var result = await CheckAsync(client, HealthStatus.Unhealthy);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        var deadline = Assert.IsType<DateTime>(client.Options?.Deadline);
        Assert.InRange(deadline - startedAt, TimeSpan.Zero, AggregatorHealthChecks.GrpcCheckDeadline + TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Not_serving_service_gets_the_failure_status_of_its_registration()
    {
        var client = new FakeHealthClient(() => GrpcCalls.Success(new HealthCheckResponse
        {
            Status = HealthCheckResponse.Types.ServingStatus.NotServing
        }));

        var result = await CheckAsync(client, HealthStatus.Unhealthy);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Theory]
    [InlineData(StatusCode.Unavailable)]
    [InlineData(StatusCode.DeadlineExceeded)]
    public async Task Rpc_failure_of_an_optional_service_only_degrades(StatusCode code)
    {
        var client = new FakeHealthClient(() => GrpcCalls.Failure<HealthCheckResponse>(code));

        var result = await CheckAsync(client, HealthStatus.Degraded);

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.IsType<RpcException>(result.Exception);
    }

    private static Task<HealthCheckResult> CheckAsync(Health.HealthClient client, HealthStatus failureStatus)
    {
        var check = new DownstreamGrpcHealthCheck(client, "reviews-api", AggregatorHealthChecks.GrpcCheckDeadline,
            TimeProvider.System, NullLogger<DownstreamGrpcHealthCheck>.Instance);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("reviews-api", check, failureStatus, ["ready"])
        };
        return check.CheckHealthAsync(context);
    }

    private sealed class FakeHealthClient(Func<AsyncUnaryCall<HealthCheckResponse>> respond) : Health.HealthClient
    {
        public CallOptions? Options { get; private set; }

        public override AsyncUnaryCall<HealthCheckResponse> CheckAsync(HealthCheckRequest request, CallOptions options)
        {
            Options = options;
            return respond();
        }
    }
}
