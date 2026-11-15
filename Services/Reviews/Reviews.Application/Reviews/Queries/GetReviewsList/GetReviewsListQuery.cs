using Reviews.Application.Common.Messaging;
using Reviews.Application.Common.Models;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Enums;

namespace Reviews.Application.Reviews.Queries.GetReviewsList;

public sealed record GetReviewsListQuery(
    long? CatalogItemId = null,
    long? RepairOrderId = null,
    long? CustomerId = null,
    ReviewType? ReviewType = null,
    int? MinRating = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<ReviewSummaryDto>>;
