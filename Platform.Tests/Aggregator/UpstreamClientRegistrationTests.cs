using System.Net;
using System.Net.Http.Headers;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Health.V1;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Aggregator.Resilience;
using Platform.Tests.ServiceDefaults;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Platform.Tests.Aggregator;

public sealed class UpstreamClientRegistrationTests
{
    private readonly StubHttpHandler _handler =
        new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

    private readonly ListLoggerProvider _logs = new();

    [Fact]
    public async Task Reviews_client_retries_once_on_top_of_replaced_defaults()
    {
        var client = CreateClient(UpstreamPolicies.Reviews);

        using var response = await client.PostAsync("http://reviews/call", new ByteArrayContent([1]));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(2, _handler.Requests.Count);
    }

    [Theory]
    [InlineData("repairs-api")]
    [InlineData("catalog-api")]
    public async Task Critical_clients_leave_retries_to_the_grpc_policy(string service)
    {
        var client = CreateClient(UpstreamPolicies.All.Single(policy => policy.Service == service));

        using var response = await client.PostAsync($"http://{service}/call", new ByteArrayContent([1]));

        Assert.Single(_handler.Requests);
    }

    [Fact]
    public async Task Circuit_opens_after_the_minimum_throughput_and_rejects_calls_without_sending_them()
    {
        var policy = UpstreamPolicies.Repairs;
        var client = CreateClient(policy);

        for (var i = 0; i < policy.CircuitBreaker.MinimumThroughput; i++)
            (await client.PostAsync("http://repairs/call", new ByteArrayContent([1]))).Dispose();

        await Assert.ThrowsAsync<BrokenCircuitException>(() =>
            client.PostAsync("http://repairs/call", new ByteArrayContent([1])));
        Assert.Equal(policy.CircuitBreaker.MinimumThroughput, _handler.Requests.Count);
        Assert.Contains(_logs.Entries, entry =>
            entry.Level == LogLevel.Error && entry.Message.StartsWith("Circuit breaker of repairs-api opened"));
    }

    [Theory]
    [InlineData("repairs-api")]
    [InlineData("catalog-api")]
    public async Task Grpc_policy_retries_an_attempt_that_timed_out(string service)
    {
        var policy = FastTimeouts(UpstreamPolicies.All.Single(policy => policy.Service == service));
        var attempts = 0;
        var handler = new StubHttpHandler(async (_, ct) =>
        {
            if (Interlocked.Increment(ref attempts) == 1)
                await Task.Delay(Timeout.Infinite, ct);
            return Serving();
        });
        var client = CreateHealthClient(policy, handler);

        var reply = await client.CheckAsync(new HealthCheckRequest(), deadline: DateTime.UtcNow.AddSeconds(20));

        Assert.Equal(HealthCheckResponse.Types.ServingStatus.Serving, reply.Status);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("1", handler.Requests[1].Headers.GetValues("grpc-previous-rpc-attempts").Single());
    }

    [Fact]
    public async Task Hanging_service_ends_as_unavailable_after_all_grpc_attempts()
    {
        var policy = FastTimeouts(UpstreamPolicies.Repairs) with
        {
            CircuitBreaker = UpstreamPolicies.Repairs.CircuitBreaker with { MinimumThroughput = 100 }
        };
        var handler = new StubHttpHandler(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return Serving();
        });
        var client = CreateHealthClient(policy, handler);

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.CheckAsync(new HealthCheckRequest(), deadline: DateTime.UtcNow.AddSeconds(30)));

        Assert.Equal(StatusCode.Unavailable, exception.StatusCode);
        Assert.Equal(policy.GrpcRetry!.MaxAttempts, handler.Requests.Count);
    }

    [Fact]
    public async Task Reviews_client_without_grpc_policy_keeps_the_timeout_for_the_http_retry()
    {
        var policy = FastTimeouts(UpstreamPolicies.Reviews);
        var handler = new StubHttpHandler(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return Serving();
        });
        var client = CreateHealthClient(policy, handler);

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.CheckAsync(new HealthCheckRequest(), deadline: DateTime.UtcNow.AddSeconds(20)));

        Assert.IsType<TimeoutRejectedException>(exception.Status.DebugException);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public void Options_carry_the_policy_values()
    {
        var services = CreateServices(UpstreamPolicies.Catalog);
        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<HttpStandardResilienceOptions>>()
            .Get($"{UpstreamPolicies.Catalog.Service}-standard");

        Assert.Equal(TimeSpan.FromSeconds(3), options.AttemptTimeout.Timeout);
        Assert.Equal(TimeSpan.FromSeconds(4), options.TotalRequestTimeout.Timeout);
        Assert.Equal(0.3, options.CircuitBreaker.FailureRatio);
        Assert.Equal(5, options.CircuitBreaker.MinimumThroughput);
        Assert.Equal(TimeSpan.FromSeconds(15), options.CircuitBreaker.BreakDuration);
    }

    [Fact]
    public void Grpc_retry_policy_retries_unavailable_with_exponential_backoff()
    {
        var config = UpstreamClientRegistration.ToServiceConfig(UpstreamPolicies.Catalog.GrpcRetry!);
        var retry = Assert.Single(config.MethodConfigs).RetryPolicy!;

        Assert.Equal(5, retry.MaxAttempts);
        Assert.Equal(2, retry.BackoffMultiplier);
        Assert.Contains(Grpc.Core.StatusCode.Unavailable, retry.RetryableStatusCodes);
    }

    private static UpstreamPolicy FastTimeouts(UpstreamPolicy policy) => policy with
    {
        AttemptTimeout = TimeSpan.FromMilliseconds(100),
        TotalRequestTimeout = TimeSpan.FromSeconds(1)
    };

    private Health.HealthClient CreateHealthClient(UpstreamPolicy policy, StubHttpHandler handler)
    {
        var services = CreateServices(policy);
        services.AddGrpcClient<Health.HealthClient>(policy.Service)
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        return services.BuildServiceProvider()
            .GetRequiredService<GrpcClientFactory>()
            .CreateClient<Health.HealthClient>(policy.Service);
    }

    private static HttpResponseMessage Serving()
    {
        var message = new HealthCheckResponse { Status = HealthCheckResponse.Types.ServingStatus.Serving }.ToByteArray();
        byte[] frame = [0, 0, 0, 0, (byte)message.Length, .. message];
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Version = HttpVersion.Version20,
            Content = new ByteArrayContent(frame)
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/grpc");
        response.TrailingHeaders.Add("grpc-status", "0");
        return response;
    }

    private HttpClient CreateClient(UpstreamPolicy policy)
        => CreateServices(policy).BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(policy.Service);

    private ServiceCollection CreateServices(UpstreamPolicy policy)
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddProvider(_logs));
        services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());
        services.AddUpstreamGrpcClient<Health.HealthClient>(policy)
            .ConfigurePrimaryHttpMessageHandler(() => _handler);
        return services;
    }
}

internal sealed class ListLoggerProvider : ILoggerProvider
{
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    public ILogger CreateLogger(string categoryName) => new ListLogger(this);

    public void Dispose()
    {
    }

    private sealed class ListLogger(ListLoggerProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lock (provider.Entries)
                provider.Entries.Add((logLevel, formatter(state, exception)));
        }
    }
}
