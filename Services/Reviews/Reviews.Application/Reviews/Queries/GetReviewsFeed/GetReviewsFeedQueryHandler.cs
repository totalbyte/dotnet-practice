using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Models;
using Reviews.Application.Reviews.Dtos;

namespace Reviews.Application.Reviews.Queries.GetReviewsFeed;

public sealed class GetReviewsFeedQueryHandler(IReviewQueries queries)
    : IRequestHandler<GetReviewsFeedQuery, CursorPage<ReviewSummaryDto>>
{
    public Task<CursorPage<ReviewSummaryDto>> Handle(GetReviewsFeedQuery query, CancellationToken cancellationToken)
    {
        var filter = new ReviewFilter(CatalogItemId: query.CatalogItemId, CustomerId: query.CustomerId);
        ObjectId? after = query.After is null ? null : ObjectId.Parse(query.After);

        return queries.GetFeedAsync(filter, after, query.Limit, cancellationToken);
    }
}
