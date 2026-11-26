using Platform.ServiceDefaults.Metrics;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Models;
using Reviews.Domain.Enums;

namespace Reviews.WebApi.Metrics;

public sealed class PublishedReviewsSnapshot(
    IServiceScopeFactory scopes,
    ReviewMetrics metrics,
    ILogger<PublishedReviewsSnapshot> logger) : MetricsSnapshotService(scopes, logger)
{
    protected override async Task RefreshAsync(IServiceProvider services, CancellationToken ct)
    {
        var queries = services.GetRequiredService<IReviewQueries>();
        var counts = new Dictionary<string, long>();

        foreach (var reviewType in Enum.GetValues<ReviewType>())
        {
            var page = await queries.GetListAsync(new ReviewFilter(ReviewType: reviewType), page: 1, pageSize: 1, ct);
            counts[reviewType.ToString()] = page.TotalCount;
        }

        metrics.UpdatePublishedReviews(counts);
    }
}
