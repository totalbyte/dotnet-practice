using Reviews.Domain.Enums;

namespace Reviews.Application.Reviews.Dtos;

public sealed record ReviewSummaryDto(
    string Id,
    ReviewType ReviewType,
    int Rating,
    string? Title,
    string Text,
    AuthorDto Author,
    long? CatalogItemId,
    string? ItemName,
    long? RepairOrderId,
    int CommentsCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int Version);
