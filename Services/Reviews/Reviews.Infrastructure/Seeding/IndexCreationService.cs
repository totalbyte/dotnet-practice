using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Domain.Enums;
using Reviews.Infrastructure.Persistence;

namespace Reviews.Infrastructure.Seeding;

public sealed class IndexCreationService(MongoDbContext context, ILogger<IndexCreationService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var names = await context.Reviews.Indexes.CreateManyAsync(ReviewIndexes(), cancellationToken);
        logger.LogInformation("Indexes of {Collection} are up to date: {Indexes}",
            MongoDbContext.ReviewsCollectionName, names);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public static IEnumerable<CreateIndexModel<Review>> ReviewIndexes()
    {
        var keys = Builders<Review>.IndexKeys;
        var filter = Builders<Review>.Filter;

        yield return new CreateIndexModel<Review>(
            keys.Ascending(r => r.CatalogItemId).Descending(r => r.CreatedAt),
            new CreateIndexOptions<Review>
            {
                Name = "ix_reviews_catalogItemId_createdAt",
                PartialFilterExpression = filter.Eq(r => r.ReviewType, ReviewType.Item)
            });

        yield return new CreateIndexModel<Review>(
            keys.Ascending(r => r.RepairOrderId),
            new CreateIndexOptions<Review>
            {
                Name = "ux_reviews_repairOrderId",
                Unique = true,
                PartialFilterExpression = filter.Exists(r => r.RepairOrderId)
            });

        yield return new CreateIndexModel<Review>(
            keys.Ascending(r => r.CatalogItemId).Ascending(r => r.Author.CustomerId),
            new CreateIndexOptions<Review>
            {
                Name = "ux_reviews_catalogItemId_customerId",
                Unique = true,
                PartialFilterExpression = filter.Eq(r => r.ReviewType, ReviewType.Item)
            });

        yield return new CreateIndexModel<Review>(
            keys.Ascending(r => r.Author.CustomerId).Descending(r => r.CreatedAt),
            new CreateIndexOptions { Name = "ix_reviews_author_createdAt" });

        yield return new CreateIndexModel<Review>(
            keys.Ascending(r => r.Rating).Descending(r => r.CreatedAt),
            new CreateIndexOptions { Name = "ix_reviews_rating_createdAt" });

        yield return new CreateIndexModel<Review>(
            keys.Text(r => r.Title).Text(r => r.Text),
            new CreateIndexOptions
            {
                Name = "ix_reviews_text",
                DefaultLanguage = "none",
                Weights = new BsonDocument { { "title", 3 }, { "text", 1 } }
            });
    }
}
