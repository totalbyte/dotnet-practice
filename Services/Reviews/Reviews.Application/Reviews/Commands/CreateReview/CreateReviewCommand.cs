using Reviews.Application.Common.Messaging;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Enums;

namespace Reviews.Application.Reviews.Commands.CreateReview;

public sealed record CreateReviewCommand(
    ReviewType? ReviewType,
    long CustomerId,
    string AuthorName,
    int Rating,
    string? Title,
    string Text,
    long? CatalogItemId = null,
    string? ItemName = null,
    IReadOnlyList<string>? Pros = null,
    IReadOnlyList<string>? Cons = null,
    long? RepairOrderId = null,
    DeviceDto? Device = null) : ICommand<string>;
