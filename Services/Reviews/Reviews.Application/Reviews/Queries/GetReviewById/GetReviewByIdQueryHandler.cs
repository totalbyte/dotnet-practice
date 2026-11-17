using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common.Exceptions;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Entities;

namespace Reviews.Application.Reviews.Queries.GetReviewById;

public sealed class GetReviewByIdQueryHandler(IReviewQueries queries)
    : IRequestHandler<GetReviewByIdQuery, ReviewDetailsDto>
{
    public async Task<ReviewDetailsDto> Handle(GetReviewByIdQuery query, CancellationToken cancellationToken)
        => await queries.GetByIdAsync(ObjectId.Parse(query.Id), cancellationToken)
           ?? throw new NotFoundException(nameof(Review), query.Id);
}
