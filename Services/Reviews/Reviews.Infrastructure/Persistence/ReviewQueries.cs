using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Models;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Entities;

namespace Reviews.Infrastructure.Persistence;

public sealed class ReviewQueries(MongoDbContext context) : IReviewQueries
{
    private static readonly BsonDocument SummaryProjection = new()
    {
        { "_id", new BsonDocument("$toString", "$_id") },
        { "reviewType", 1 },
        { "rating", 1 },
        { "title", 1 },
        { "text", 1 },
        { "author", 1 },
        { "catalogItemId", 1 },
        { "itemName", 1 },
        { "repairOrderId", 1 },
        { "commentsCount", IfNull("$commentsCount", 0) },
        { "createdAt", 1 },
        { "updatedAt", 1 },
        { "version", 1 }
    };

    private static readonly BsonDocument DetailsProjection = new()
    {
        { "_id", new BsonDocument("$toString", "$_id") },
        { "reviewType", 1 },
        { "rating", 1 },
        { "title", 1 },
        { "text", 1 },
        { "author", 1 },
        { "catalogItemId", 1 },
        { "itemName", 1 },
        { "pros", IfNull("$pros", new BsonArray()) },
        { "cons", IfNull("$cons", new BsonArray()) },
        { "repairOrderId", 1 },
        { "device", 1 },
        { "technicianName", "$technician.displayName" },
        { "usedItems", IfNull("$usedItems", new BsonArray()) },
        { "photos", IfNull("$photos", new BsonArray()) },
        { "comments", IfNull("$comments", new BsonArray()) },
        { "commentsCount", IfNull("$commentsCount", 0) },
        { "staffReply", 1 },
        { "discussionId", new BsonDocument("$toString", "$discussionId") },
        { "createdAt", 1 },
        { "updatedAt", 1 },
        { "version", 1 }
    };

    private static readonly BsonDocument CommentsProjection = new()
    {
        { "_id", 0 },
        { "comments", IfNull("$comments", new BsonArray()) }
    };

    private readonly IMongoCollection<Review> _reviews = context.Reviews;

    public async Task<ReviewDetailsDto?> GetByIdAsync(ObjectId id, CancellationToken ct)
        => await _reviews.Aggregate()
            .Match(ReviewFilters.ActiveById(id))
            .Project<ReviewDetailsDto>(DetailsProjection)
            .FirstOrDefaultAsync(ct);

    public async Task<PagedResult<ReviewSummaryDto>> GetListAsync(
        ReviewFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var match = ReviewFilters.From(filter);

        var totalCount = await _reviews.CountDocumentsAsync(match, cancellationToken: ct);
        var items = await _reviews.Aggregate()
            .Match(match)
            .Sort(Builders<Review>.Sort.Descending(r => r.CreatedAt).Descending(r => r.Id))
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .Project<ReviewSummaryDto>(SummaryProjection)
            .ToListAsync(ct);

        return new PagedResult<ReviewSummaryDto>(items, page, pageSize, totalCount);
    }

    public async Task<CursorPage<ReviewSummaryDto>> GetFeedAsync(
        ReviewFilter filter, ObjectId? after, int limit, CancellationToken ct)
    {
        var match = ReviewFilters.From(filter);
        if (after is { } cursor)
            match &= Builders<Review>.Filter.Lt(r => r.Id, cursor);

        var items = await _reviews.Aggregate()
            .Match(match)
            .Sort(Builders<Review>.Sort.Descending(r => r.Id))
            .Limit(limit + 1)
            .Project<ReviewSummaryDto>(SummaryProjection)
            .ToListAsync(ct);

        var hasMore = items.Count > limit;
        if (hasMore)
            items.RemoveAt(limit);

        return new CursorPage<ReviewSummaryDto>(items, hasMore ? items[^1].Id : null);
    }

    public async Task<IReadOnlyList<CommentDto>?> GetCommentsAsync(ObjectId reviewId, CancellationToken ct)
    {
        var view = await _reviews.Find(ReviewFilters.ActiveById(reviewId))
            .Project<ReviewCommentsView>(CommentsProjection)
            .FirstOrDefaultAsync(ct);

        return view?.Comments;
    }

    private static BsonDocument IfNull(string field, BsonValue fallback)
        => new("$ifNull", new BsonArray { field, fallback });

    private sealed class ReviewCommentsView
    {
        public List<CommentDto> Comments { get; set; } = [];
    }
}
