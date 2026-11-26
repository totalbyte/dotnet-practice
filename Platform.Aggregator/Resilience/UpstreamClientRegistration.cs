using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Platform.Aggregator.Clients;
using Polly;

namespace Platform.Aggregator.Resilience;

public static class UpstreamClientRegistration
{
    public const string LoggerCategory = "Platform.Aggregator.Resilience";

    public static IHttpClientBuilder AddUpstreamGrpcClient<TClient>(this IServiceCollection services, UpstreamPolicy policy)
        where TClient : class
    {
        var client = services.AddGrpcClient<TClient>(policy.Service, options =>
            options.Address = UpstreamServices.GrpcAddress(policy.Service));

        client.RemoveAllResilienceHandlers();

        if (policy.GrpcRetry is { } grpcRetry)
        {
            client.ConfigureChannel(channel => channel.ServiceConfig = ToServiceConfig(grpcRetry));
            client.AddHttpMessageHandler(() => new AttemptTimeoutHandler());
        }

        client.AddStandardResilienceHandler()
            .Configure((options, serviceProvider) => Apply(options, policy,
                serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(LoggerCategory)));

        return client;
    }

    public static ServiceConfig ToServiceConfig(GrpcRetrySettings retry) => new()
    {
        MethodConfigs =
        {
            new MethodConfig
            {
                Names = { MethodName.Default },
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = retry.MaxAttempts,
                    InitialBackoff = retry.InitialBackoff,
                    MaxBackoff = retry.MaxBackoff,
                    BackoffMultiplier = retry.BackoffMultiplier,
                    RetryableStatusCodes = { StatusCode.Unavailable, StatusCode.ResourceExhausted }
                }
            }
        }
    };

    public static void Apply(HttpStandardResilienceOptions options, UpstreamPolicy policy, ILogger logger)
    {
        options.AttemptTimeout.Timeout = policy.AttemptTimeout;
        options.TotalRequestTimeout.Timeout = policy.TotalRequestTimeout;

        if (policy.HttpRetry is { } retry)
        {
            options.Retry.MaxRetryAttempts = retry.MaxRetryAttempts;
            options.Retry.Delay = retry.Delay;
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;
            options.Retry.DisableFor(HttpMethod.Put, HttpMethod.Patch, HttpMethod.Delete);
        }
        else
        {
            options.Retry.DisableForUnsafeHttpMethods();
        }

        var breaker = policy.CircuitBreaker;
        options.CircuitBreaker.FailureRatio = breaker.FailureRatio;
        options.CircuitBreaker.MinimumThroughput = breaker.MinimumThroughput;
        options.CircuitBreaker.SamplingDuration = breaker.SamplingDuration;
        options.CircuitBreaker.BreakDuration = breaker.BreakDuration;
        options.CircuitBreaker.OnOpened = args =>
        {
            logger.LogError(args.Outcome.Exception,
                "Circuit breaker of {Service} opened for {BreakDuration}: {Reason}",
                policy.Service, args.BreakDuration, Describe(args.Outcome));
            return ValueTask.CompletedTask;
        };
        options.CircuitBreaker.OnHalfOpened = _ =>
        {
            logger.LogInformation("Circuit breaker of {Service} is half-open, a trial call is let through",
                policy.Service);
            return ValueTask.CompletedTask;
        };
        options.CircuitBreaker.OnClosed = _ =>
        {
            logger.LogInformation("Circuit breaker of {Service} closed, calls flow again", policy.Service);
            return ValueTask.CompletedTask;
        };
    }

    private static string Describe(Outcome<HttpResponseMessage> outcome)
        => outcome.Exception?.GetType().Name
           ?? (outcome.Result is { } response ? $"HTTP {(int)response.StatusCode}" : "unknown");
}
