using MediatR;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Models;
using Reviews.Application.Reviews.Dtos;

namespace Reviews.Application.Reviews.Queries.GetRepairOrderReviews;

public sealed class GetRepairOrderReviewsQueryHandler(IReviewQueries queries)
    : IRequestHandler<GetRepairOrderReviewsQuery, IReadOnlyList<ReviewSummaryDto>>
{
    public async Task<IReadOnlyList<ReviewSummaryDto>> Handle(
        GetRepairOrderReviewsQuery query, CancellationToken cancellationToken)
    {
        var pages = await Task.WhenAll(
            query.CatalogItemIds.Distinct()
                .Select(itemId => queries.GetListAsync(
                    new ReviewFilter(CatalogItemId: itemId), 1, query.ItemReviewsLimit, cancellationToken))
                .Prepend(queries.GetListAsync(
                    new ReviewFilter(RepairOrderId: query.RepairOrderId), 1, query.RepairReviewsLimit,
                    cancellationToken)));

        return
        [
            .. pages.SelectMany(page => page.Items)
                .DistinctBy(review => review.Id)
                .OrderByDescending(review => review.CreatedAt)
        ];
    }
}
