using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Domain.Enums;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Interfaces;

namespace Reviews.Infrastructure.Persistence;

public sealed class ReviewRepository(MongoDbContext context) : IReviewRepository
{
    private static readonly Lazy<string[]> MappedElements = new(() => BsonClassMap
        .LookupClassMap(typeof(Review))
        .AllMemberMaps
        .Select(m => m.ElementName)
        .Where(name => name != "_id")
        .ToArray());

    private readonly IMongoCollection<Review> _reviews = context.Reviews;

    public async Task<Review?> GetByIdAsync(ObjectId id, CancellationToken ct = default)
        => await _reviews.Find(ReviewFilters.ActiveById(id)).FirstOrDefaultAsync(ct);

    public Task<bool> ExistsForCatalogItemAsync(long catalogItemId, long customerId, CancellationToken ct = default)
        => _reviews.Find(r => r.ReviewType == ReviewType.Item
                              && r.CatalogItemId == catalogItemId
                              && r.Author.CustomerId == customerId)
            .AnyAsync(ct);

    public Task<bool> ExistsForRepairOrderAsync(long repairOrderId, CancellationToken ct = default)
        => _reviews.Find(r => r.RepairOrderId == repairOrderId).AnyAsync(ct);

    public Task AddAsync(Review review, CancellationToken ct = default)
        => _reviews.InsertOneAsync(review, cancellationToken: ct);

    public async Task UpdateAsync(Review review, CancellationToken ct = default)
    {
        var expectedVersion = review.Version;
        review.BumpVersion();

        var filter = Builders<Review>.Filter.Eq(r => r.Id, review.Id)
                     & Builders<Review>.Filter.Eq(r => r.Version, expectedVersion);

        var result = await _reviews.UpdateOneAsync(filter, BuildUpdate(review), cancellationToken: ct);
        if (result.MatchedCount == 0)
            throw new ConflictException(
                $"Review {review.Id} was modified by another request (expected version {expectedVersion})");
    }

    private static UpdateDefinition<Review> BuildUpdate(Review review)
    {
        var fields = review.ToBsonDocument();
        fields.Remove("_id");

        var update = new BsonDocument("$set", fields);

        var removed = MappedElements.Value.Where(name => !fields.Contains(name)).ToArray();
        if (removed.Length > 0)
            update.Add("$unset", new BsonDocument(removed.Select(name => new BsonElement(name, string.Empty))));

        return update;
    }
}
