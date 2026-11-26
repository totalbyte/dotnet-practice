using Platform.Aggregator.Resilience;

namespace Platform.Tests.Aggregator;

public sealed class UpstreamPoliciesTests
{
    private static readonly TimeSpan GatewayActivityTimeout = TimeSpan.FromSeconds(30);

    public static TheoryData<string> Services => [.. UpstreamPolicies.All.Select(policy => policy.Service)];

    [Theory]
    [MemberData(nameof(Services))]
    public void Every_policy_is_consistent(string service)
        => Assert.Empty(Policy(service).Validate());

    [Fact]
    public void Critical_services_retry_more_and_break_sooner_than_reviews()
    {
        var reviews = UpstreamPolicies.Reviews;
        Assert.False(reviews.IsCritical);

        foreach (var critical in UpstreamPolicies.All.Where(policy => policy.IsCritical))
        {
            Assert.NotNull(critical.GrpcRetry);
            Assert.True(critical.GrpcRetry.MaxAttempts - 1 > reviews.HttpRetry!.MaxRetryAttempts);
            Assert.True(critical.CircuitBreaker.FailureRatio < reviews.CircuitBreaker.FailureRatio);
            Assert.True(critical.CallDeadline > reviews.CallDeadline);
        }
    }

    [Fact]
    public void Card_is_answered_before_the_gateway_gives_up()
    {
        var worstCase = UpstreamPolicies.Repairs.CallDeadline
                        + Max(UpstreamPolicies.Catalog.CallDeadline, UpstreamPolicies.Reviews.CallDeadline);

        Assert.True(worstCase < GatewayActivityTimeout);
    }

    [Fact]
    public void Combining_http_and_grpc_retries_is_rejected()
    {
        var policy = UpstreamPolicies.Catalog with
        {
            HttpRetry = new HttpRetrySettings(1, TimeSpan.FromMilliseconds(100))
        };

        Assert.Contains(policy.Validate(), error => error.Contains("cannot be combined"));
    }

    [Fact]
    public void Deadline_without_room_for_a_retry_after_a_timeout_is_rejected()
    {
        var policy = UpstreamPolicies.Repairs with { CallDeadline = TimeSpan.FromSeconds(10) };

        Assert.Contains(policy.Validate(), error => error.Contains("second full attempt"));
    }

    [Fact]
    public void Short_sampling_window_is_rejected()
    {
        var policy = UpstreamPolicies.Reviews with
        {
            CircuitBreaker = UpstreamPolicies.Reviews.CircuitBreaker with { SamplingDuration = TimeSpan.FromSeconds(3) }
        };

        Assert.Contains(policy.Validate(), error => error.Contains("sampling duration"));
    }

    private static UpstreamPolicy Policy(string service) => UpstreamPolicies.All.Single(policy => policy.Service == service);

    private static TimeSpan Max(TimeSpan left, TimeSpan right) => left > right ? left : right;
}
