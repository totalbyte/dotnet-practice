using Reviews.Application.Common.Messaging;
using Reviews.Application.Reviews.Dtos;

namespace Reviews.Application.Reviews.Queries.GetReviewComments;

public sealed record GetReviewCommentsQuery(string ReviewId) : IQuery<IReadOnlyList<CommentDto>>;
