using MongoDB.Bson;
using Reviews.Domain.Entities;

namespace Reviews.Domain.Interfaces;

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(ObjectId id, CancellationToken ct = default);

    Task<bool> ExistsForCatalogItemAsync(long catalogItemId, long customerId, CancellationToken ct = default);

    Task<bool> ExistsForRepairOrderAsync(long repairOrderId, CancellationToken ct = default);

    Task AddAsync(Review review, CancellationToken ct = default);

    Task UpdateAsync(Review review, CancellationToken ct = default);
}
