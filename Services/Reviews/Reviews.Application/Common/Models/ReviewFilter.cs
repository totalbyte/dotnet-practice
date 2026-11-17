using Reviews.Domain.Enums;

namespace Reviews.Application.Common.Models;

public sealed record ReviewFilter(
    long? CatalogItemId = null,
    long? RepairOrderId = null,
    long? CustomerId = null,
    ReviewType? ReviewType = null,
    int? MinRating = null,
    string? Search = null);
