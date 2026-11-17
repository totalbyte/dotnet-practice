using Reviews.Application.Common.Messaging;
using Reviews.Application.Reviews.Dtos;

namespace Reviews.Application.Reviews.Commands.ReplyToReview;

public sealed record ReplyToReviewCommand(string ReviewId, string AuthorName, string Text) : ICommand<ReviewDto>;
