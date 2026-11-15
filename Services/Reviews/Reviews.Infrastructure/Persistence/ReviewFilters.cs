using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Application.Common.Models;
using Reviews.Domain.Entities;

namespace Reviews.Infrastructure.Persistence;

internal static class ReviewFilters
{
    private static FilterDefinitionBuilder<Review> Filter => Builders<Review>.Filter;

    public static FilterDefinition<Review> Active => Filter.Ne(r => r.IsDeleted, true);

    public static FilterDefinition<Review> ActiveById(ObjectId id) => Filter.Eq(r => r.Id, id) & Active;

    public static FilterDefinition<Review> From(ReviewFilter criteria)
    {
        var filter = Active;

        if (!string.IsNullOrWhiteSpace(criteria.Search))
            filter = Filter.Text(criteria.Search) & filter;
        if (criteria.CatalogItemId is { } catalogItemId)
            filter &= Filter.Eq(r => r.CatalogItemId, catalogItemId);
        if (criteria.RepairOrderId is { } repairOrderId)
            filter &= Filter.Eq(r => r.RepairOrderId, repairOrderId);
        if (criteria.CustomerId is { } customerId)
            filter &= Filter.Eq(r => r.Author.CustomerId, customerId);
        if (criteria.ReviewType is { } reviewType)
            filter &= Filter.Eq(r => r.ReviewType, reviewType);
        if (criteria.MinRating is { } minRating)
            filter &= Filter.Gte(r => r.Rating, minRating);

        return filter;
    }
}
