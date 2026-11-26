using Platform.Aggregator.Clients;

namespace Platform.Aggregator.Resilience;

public static class UpstreamPolicies
{
    public static readonly UpstreamPolicy Repairs = new()
    {
        Service = UpstreamServices.Repairs,
        IsCritical = true,
        CallDeadline = TimeSpan.FromSeconds(11),
        AttemptTimeout = TimeSpan.FromSeconds(5),
        TotalRequestTimeout = TimeSpan.FromSeconds(6),
        GrpcRetry = new GrpcRetrySettings(
            MaxAttempts: 4,
            InitialBackoff: TimeSpan.FromMilliseconds(500),
            MaxBackoff: TimeSpan.FromSeconds(4),
            BackoffMultiplier: 2),
        CircuitBreaker = new CircuitBreakerSettings(
            FailureRatio: 0.3,
            MinimumThroughput: 5,
            SamplingDuration: TimeSpan.FromSeconds(30),
            BreakDuration: TimeSpan.FromSeconds(10))
    };

    public static readonly UpstreamPolicy Catalog = new()
    {
        Service = UpstreamServices.Catalog,
        IsCritical = true,
        CallDeadline = TimeSpan.FromSeconds(8),
        AttemptTimeout = TimeSpan.FromSeconds(3),
        TotalRequestTimeout = TimeSpan.FromSeconds(4),
        GrpcRetry = new GrpcRetrySettings(
            MaxAttempts: 5,
            InitialBackoff: TimeSpan.FromMilliseconds(200),
            MaxBackoff: TimeSpan.FromSeconds(2),
            BackoffMultiplier: 2),
        CircuitBreaker = new CircuitBreakerSettings(
            FailureRatio: 0.3,
            MinimumThroughput: 5,
            SamplingDuration: TimeSpan.FromSeconds(30),
            BreakDuration: TimeSpan.FromSeconds(15))
    };

    // The non-critical client gives up fast: instead of the gRPC RetryPolicy it has one Polly HTTP retry
    // (the two are incompatible on one channel). Retrying POST is safe because these calls only read.
    public static readonly UpstreamPolicy Reviews = new()
    {
        Service = UpstreamServices.Reviews,
        IsCritical = false,
        CallDeadline = TimeSpan.FromSeconds(6),
        AttemptTimeout = TimeSpan.FromSeconds(2),
        TotalRequestTimeout = TimeSpan.FromSeconds(5),
        HttpRetry = new HttpRetrySettings(MaxRetryAttempts: 1, Delay: TimeSpan.FromMilliseconds(200)),
        CircuitBreaker = new CircuitBreakerSettings(
            FailureRatio: 0.7,
            MinimumThroughput: 5,
            SamplingDuration: TimeSpan.FromSeconds(30),
            BreakDuration: TimeSpan.FromSeconds(15))
    };

    public static IReadOnlyList<UpstreamPolicy> All { get; } = [Repairs, Catalog, Reviews];
}
