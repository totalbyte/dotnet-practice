using Reviews.Application.Common.Messaging;
using Reviews.Application.Reviews.Dtos;

namespace Reviews.Application.Reviews.Queries.GetRepairOrderReviews;

public sealed record GetRepairOrderReviewsQuery(
    long RepairOrderId,
    IReadOnlyList<long> CatalogItemIds,
    int RepairReviewsLimit = GetRepairOrderReviewsQuery.DefaultRepairReviewsLimit,
    int ItemReviewsLimit = GetRepairOrderReviewsQuery.DefaultItemReviewsLimit)
    : IQuery<IReadOnlyList<ReviewSummaryDto>>
{
    public const int DefaultRepairReviewsLimit = 5;
    public const int DefaultItemReviewsLimit = 3;
    public const int MaxCatalogItems = 50;
}
