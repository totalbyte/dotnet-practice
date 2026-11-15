using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common.Exceptions;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Entities;

namespace Reviews.Application.Reviews.Queries.GetReviewComments;

public sealed class GetReviewCommentsQueryHandler(IReviewQueries queries)
    : IRequestHandler<GetReviewCommentsQuery, IReadOnlyList<CommentDto>>
{
    public async Task<IReadOnlyList<CommentDto>> Handle(GetReviewCommentsQuery query, CancellationToken cancellationToken)
        => await queries.GetCommentsAsync(ObjectId.Parse(query.ReviewId), cancellationToken)
           ?? throw new NotFoundException(nameof(Review), query.ReviewId);
}
