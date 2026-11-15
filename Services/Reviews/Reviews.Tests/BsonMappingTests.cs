using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Entities;
using Reviews.Domain.Enums;
using Reviews.Domain.ValueObjects;
using Reviews.Infrastructure.Persistence;
using Reviews.Infrastructure.Seeding;

namespace Reviews.Tests;

public sealed class BsonMappingTests
{
    public BsonMappingTests() => MongoMappings.Register();

    [Fact]
    public void Item_review_is_serialized_with_camel_case_fields_and_bson_types()
    {
        var review = Review.ForItem(3, "Battery", new Author(2, "Andrii"), 5, null, "Works", ["Fast"], null);

        var document = review.ToBsonDocument();

        Assert.Equal(review.Id, document["_id"].AsObjectId);
        Assert.Equal("item", document["reviewType"].AsString);
        Assert.Equal(BsonType.Int32, document["rating"].BsonType);
        Assert.Equal(BsonType.Int64, document["catalogItemId"].BsonType);
        Assert.Equal(BsonType.Int64, document["author"]["customerId"].BsonType);
        Assert.Equal("Andrii", document["author"]["displayName"].AsString);
        Assert.Equal(new BsonArray { "Fast" }, document["pros"].AsBsonArray);
        Assert.Equal(1, document["version"].AsInt32);
        Assert.Equal(BsonType.DateTime, document["createdAt"].BsonType);
    }

    [Fact]
    public void Empty_and_default_fields_are_not_written()
    {
        var review = Review.ForItem(3, "Battery", new Author(2, "Andrii"), 5, null, "Works");

        var names = review.ToBsonDocument().Names.ToHashSet();

        string[] expected =
            ["_id", "createdAt", "version", "reviewType", "rating", "text", "author", "catalogItemId", "itemName"];
        Assert.Equal(expected.ToHashSet(), names);
    }

    [Fact]
    public void Repair_review_with_comments_and_reply_round_trips()
    {
        var review = Review.ForRepair(9, new Device("Laptop", null, "X15"), new Author(1, "Olena"), 4, "Title", "Fixed");
        review.AddComment("Staff", "Thanks", isStaff: true);
        review.ReplyAsStaff("Service center", "You are welcome");

        var document = review.ToBsonDocument();
        var restored = BsonSerializer.Deserialize<Review>(document);

        Assert.Equal("repair", document["reviewType"].AsString);
        Assert.False(document["device"].AsBsonDocument.Contains("brand"));
        Assert.True(document["comments"][0]["isStaff"].AsBoolean);
        Assert.Equal(1, document["commentsCount"].AsInt32);
        Assert.Equal(review.Device, restored.Device);
        Assert.Equal(review.Author, restored.Author);
        Assert.Equal(review.StaffReply!.Text, restored.StaffReply!.Text);
        Assert.Single(restored.Comments);
        Assert.Equal(ReviewType.Repair, restored.ReviewType);
    }

    [Fact]
    public void Seed_documents_deserialize_into_domain_entities()
    {
        var documents = ReviewSeeder.LoadDocuments();

        var reviews = documents.Select(d => BsonSerializer.Deserialize<Review>(d)).ToList();

        Assert.Equal(7, reviews.Count);
        Assert.Contains(reviews, r => r.ReviewType == ReviewType.Repair && r.Device is not null);
        Assert.Contains(reviews, r => r.ReviewType == ReviewType.Item && r.Pros.Count > 0);
        Assert.All(reviews, r => Assert.True(r.Version >= 1));
    }

    [Fact]
    public void Projected_document_deserializes_into_summary_dto()
    {
        var document = new BsonDocument
        {
            { "_id", "671000000000000000000002" },
            { "reviewType", "item" },
            { "rating", 4 },
            { "text", "Good" },
            { "author", new BsonDocument { { "customerId", 1L }, { "displayName", "Olena" } } },
            { "catalogItemId", 1L },
            { "itemName", "Screen" },
            { "commentsCount", 0 },
            { "createdAt", new DateTime(2026, 10, 5, 18, 0, 0, DateTimeKind.Utc) },
            { "version", 2 }
        };

        var dto = BsonSerializer.Deserialize<ReviewSummaryDto>(document);

        Assert.Equal("671000000000000000000002", dto.Id);
        Assert.Equal(ReviewType.Item, dto.ReviewType);
        Assert.Equal(new AuthorDto(1, "Olena"), dto.Author);
        Assert.Null(dto.RepairOrderId);
        Assert.Equal(2, dto.Version);
    }
}
