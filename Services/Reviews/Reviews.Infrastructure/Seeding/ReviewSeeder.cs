using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Reviews.Infrastructure.Persistence;

namespace Reviews.Infrastructure.Seeding;

public sealed class ReviewSeeder(MongoDbContext context, ILogger<ReviewSeeder> logger) : IDataSeeder
{
    public const string ResourceName = "Reviews.Infrastructure.Seeding.Data.reviews.json";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var collection = context.Database.GetCollection<BsonDocument>(MongoDbContext.ReviewsCollectionName);

        if (await collection.EstimatedDocumentCountAsync(cancellationToken: ct) > 0)
        {
            logger.LogInformation("Collection {Collection} already contains data, seeding skipped",
                MongoDbContext.ReviewsCollectionName);
            return;
        }

        var documents = LoadDocuments();
        await collection.InsertManyAsync(documents, cancellationToken: ct);

        logger.LogInformation("Collection {Collection} seeded with {Count} documents",
            MongoDbContext.ReviewsCollectionName, documents.Count);
    }

    public static IReadOnlyList<BsonDocument> LoadDocuments()
    {
        using var stream = typeof(ReviewSeeder).Assembly.GetManifestResourceStream(ResourceName)
                           ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' is missing");
        using var reader = new StreamReader(stream);

        return BsonSerializer.Deserialize<BsonArray>(reader.ReadToEnd())
            .Select(value => value.AsBsonDocument)
            .ToList();
    }
}
