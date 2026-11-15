using Reviews.Application.Common.Messaging;
using Reviews.Application.Reviews.Dtos;

namespace Reviews.Application.Reviews.Commands.UpdateReview;

public sealed record UpdateReviewCommand(
    string Id,
    int ExpectedVersion,
    int Rating,
    string? Title,
    string Text,
    IReadOnlyList<string>? Pros = null,
    IReadOnlyList<string>? Cons = null) : ICommand<ReviewDto>;
