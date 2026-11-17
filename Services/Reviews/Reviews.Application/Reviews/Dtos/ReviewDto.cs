using Reviews.Domain.Enums;

namespace Reviews.Application.Reviews.Dtos;

public sealed record ReviewDto(
    string Id,
    ReviewType ReviewType,
    int Rating,
    string? Title,
    string Text,
    AuthorDto Author,
    long? CatalogItemId,
    string? ItemName,
    IReadOnlyList<string> Pros,
    IReadOnlyList<string> Cons,
    long? RepairOrderId,
    DeviceDto? Device,
    IReadOnlyList<CommentDto> Comments,
    int CommentsCount,
    StaffReplyDto? StaffReply,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int Version);
