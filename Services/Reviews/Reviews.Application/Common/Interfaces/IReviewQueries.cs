using MongoDB.Bson;
using Reviews.Application.Common.Models;
using Reviews.Application.Reviews.Dtos;

namespace Reviews.Application.Common.Interfaces;

public interface IReviewQueries
{
    Task<ReviewDetailsDto?> GetByIdAsync(ObjectId id, CancellationToken ct);

    Task<PagedResult<ReviewSummaryDto>> GetListAsync(ReviewFilter filter, int page, int pageSize, CancellationToken ct);

    Task<CursorPage<ReviewSummaryDto>> GetFeedAsync(ReviewFilter filter, ObjectId? after, int limit, CancellationToken ct);

    Task<IReadOnlyList<CommentDto>?> GetCommentsAsync(ObjectId reviewId, CancellationToken ct);
}
