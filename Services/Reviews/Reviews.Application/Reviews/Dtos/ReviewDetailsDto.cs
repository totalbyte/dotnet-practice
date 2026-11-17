using Reviews.Domain.Enums;

namespace Reviews.Application.Reviews.Dtos;

public sealed record ReviewDetailsDto(
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
    string? TechnicianName,
    IReadOnlyList<UsedItemDto> UsedItems,
    IReadOnlyList<string> Photos,
    IReadOnlyList<CommentDto> Comments,
    int CommentsCount,
    StaffReplyDto? StaffReply,
    string? DiscussionId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int Version);
