using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Platform.ServiceDefaults.HealthChecks;
using StackExchange.Redis;

namespace Platform.Tests.ServiceDefaults;

public sealed class RedisCacheHealthCheckTests
{
    private readonly IConnectionMultiplexer _redis = Substitute.For<IConnectionMultiplexer>();
    private readonly IDatabase _database = Substitute.For<IDatabase>();

    public RedisCacheHealthCheckTests() => _redis.GetDatabase().Returns(_database);

    [Fact]
    public async Task Disconnected_redis_is_reported_with_the_failure_status_without_waiting()
    {
        _redis.IsConnected.Returns(false);

        var result = await CheckAsync();

        Assert.Equal(HealthStatus.Degraded, result.Status);
        await _database.DidNotReceive().PingAsync(Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task Answered_ping_is_healthy_with_latency()
    {
        _redis.IsConnected.Returns(true);
        _database.PingAsync(Arg.Any<CommandFlags>()).Returns(TimeSpan.FromMilliseconds(1.25));

        var result = await CheckAsync();

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal(1.2, result.Data["latencyMs"]);
    }

    [Fact]
    public async Task Failed_ping_is_degraded()
    {
        _redis.IsConnected.Returns(true);
        _database.PingAsync(Arg.Any<CommandFlags>())
            .ThrowsAsync(new RedisTimeoutException("Timeout performing PING", CommandStatus.Sent));

        var result = await CheckAsync();

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.IsType<RedisTimeoutException>(result.Exception);
    }

    private Task<HealthCheckResult> CheckAsync()
    {
        var check = new RedisCacheHealthCheck(_redis);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("redis-cache", check, HealthStatus.Degraded, ["ready"])
        };
        return check.CheckHealthAsync(context);
    }
}
