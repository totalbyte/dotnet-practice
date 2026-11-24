namespace Platform.Aggregator.Services;

public sealed class AggregatorOptions
{
    public const string SectionName = "Aggregator";

    public TimeSpan CallDeadline { get; set; } = TimeSpan.FromSeconds(8);

    public TimeSpan CardCacheTimeToLive { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan CacheRetryAfter { get; set; } = TimeSpan.FromSeconds(30);

    public int RepairReviewsLimit { get; set; } = 5;

    public int ItemReviewsLimit { get; set; } = 3;
}
