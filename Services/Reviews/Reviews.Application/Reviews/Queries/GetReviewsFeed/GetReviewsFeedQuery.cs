using Reviews.Application.Common.Messaging;
using Reviews.Application.Common.Models;
using Reviews.Application.Reviews.Dtos;

namespace Reviews.Application.Reviews.Queries.GetReviewsFeed;

public sealed record GetReviewsFeedQuery(
    long? CatalogItemId = null,
    long? CustomerId = null,
    string? After = null,
    int Limit = 20) : IQuery<CursorPage<ReviewSummaryDto>>;
