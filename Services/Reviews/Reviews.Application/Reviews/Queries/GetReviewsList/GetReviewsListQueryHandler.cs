using MediatR;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Models;
using Reviews.Application.Reviews.Dtos;

namespace Reviews.Application.Reviews.Queries.GetReviewsList;

public sealed class GetReviewsListQueryHandler(IReviewQueries queries)
    : IRequestHandler<GetReviewsListQuery, PagedResult<ReviewSummaryDto>>
{
    public Task<PagedResult<ReviewSummaryDto>> Handle(GetReviewsListQuery query, CancellationToken cancellationToken)
    {
        var filter = new ReviewFilter(
            query.CatalogItemId, query.RepairOrderId, query.CustomerId, query.ReviewType, query.MinRating, query.Search);

        return queries.GetListAsync(filter, query.Page, query.PageSize, cancellationToken);
    }
}
