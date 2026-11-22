using Platform.Aggregator.Clients.Contracts;

namespace Platform.Aggregator.Clients;

public sealed class ReviewsClient(HttpClient http, ILogger<ReviewsClient> logger) : ServiceClient(http, logger)
{
    public const int RepairReviewsLimit = 5;
    public const int ItemReviewsLimit = 3;

    public override string ServiceName => "reviews-api";

    public async Task<IReadOnlyList<ReviewResponse>> GetForRepairOrderAsync(
        long repairOrderId, IReadOnlyCollection<long> catalogItemIds, CancellationToken cancellationToken)
    {
        var pages = await Task.WhenAll(
            catalogItemIds
                .Select(id => GetPageAsync(
                    $"/api/reviews?catalogItemId={id}&pageSize={ItemReviewsLimit}", $"catalog item {id}", cancellationToken))
                .Prepend(GetPageAsync(
                    $"/api/reviews?repairOrderId={repairOrderId}&pageSize={RepairReviewsLimit}",
                    $"repair order {repairOrderId}", cancellationToken)));

        return [.. pages.SelectMany(page => page).DistinctBy(review => review.Id)];
    }

    private async Task<IReadOnlyList<ReviewResponse>> GetPageAsync(
        string uri, string target, CancellationToken cancellationToken)
        => (await GetOrDefaultAsync<ReviewPageResponse>(uri, target, cancellationToken))?.Items ?? [];
}
