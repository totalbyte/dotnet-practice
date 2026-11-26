namespace Platform.Aggregator.Resilience;

public sealed record UpstreamPolicy
{
    public required string Service { get; init; }

    public required bool IsCritical { get; init; }

    public required TimeSpan CallDeadline { get; init; }

    public required TimeSpan AttemptTimeout { get; init; }

    public required TimeSpan TotalRequestTimeout { get; init; }

    public GrpcRetrySettings? GrpcRetry { get; init; }

    public HttpRetrySettings? HttpRetry { get; init; }

    public required CircuitBreakerSettings CircuitBreaker { get; init; }

    public IEnumerable<string> Validate()
    {
        if (GrpcRetry is not null && HttpRetry is not null)
            yield return $"{Service}: HTTP retries cannot be combined with the gRPC retry policy on one channel";

        if (TotalRequestTimeout < AttemptTimeout * (1 + (HttpRetry?.MaxRetryAttempts ?? 0)))
            yield return $"{Service}: the total timeout does not leave room for every HTTP attempt";

        if (CallDeadline <= TotalRequestTimeout)
            yield return $"{Service}: the gRPC deadline must outlast the HTTP pipeline";

        if (GrpcRetry is { } retry && CallDeadline < AttemptTimeout * 2 + retry.InitialBackoff)
            yield return $"{Service}: the gRPC deadline must fit a second full attempt after a timeout";

        if (CircuitBreaker.SamplingDuration < AttemptTimeout * 2)
            yield return $"{Service}: the sampling duration must be at least twice the attempt timeout";
    }
}

public sealed record GrpcRetrySettings(
    int MaxAttempts, TimeSpan InitialBackoff, TimeSpan MaxBackoff, double BackoffMultiplier);

public sealed record HttpRetrySettings(int MaxRetryAttempts, TimeSpan Delay);

public sealed record CircuitBreakerSettings(
    double FailureRatio, int MinimumThroughput, TimeSpan SamplingDuration, TimeSpan BreakDuration);
