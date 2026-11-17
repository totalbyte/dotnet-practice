using MongoDB.Bson;
using Reviews.Application.Common.Exceptions;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;

namespace Reviews.Application.Reviews;

internal static class ReviewRepositoryExtensions
{
    public static async Task<Review> GetRequiredAsync(this IReviewRepository reviews, string id, CancellationToken ct)
        => await reviews.GetByIdAsync(ObjectId.Parse(id), ct) ?? throw new NotFoundException(nameof(Review), id);
}
