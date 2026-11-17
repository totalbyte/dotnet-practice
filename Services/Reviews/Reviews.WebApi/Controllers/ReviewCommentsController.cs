using Microsoft.AspNetCore.Mvc;
using Reviews.Application.Reviews.Commands.AddComment;
using Reviews.Application.Reviews.Dtos;
using Reviews.Application.Reviews.Queries.GetReviewComments;
using Reviews.WebApi.Contracts;

namespace Reviews.WebApi.Controllers;

[Route("api/reviews/{reviewId}/comments")]
public sealed class ReviewCommentsController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CommentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CommentDto>>> GetList(string reviewId, CancellationToken ct)
        => Ok(await Mediator.Send(new GetReviewCommentsQuery(reviewId), ct));

    [HttpPost]
    [ProducesResponseType<CommentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CommentDto>> Add(string reviewId, AddCommentRequest request, CancellationToken ct)
    {
        var comment = await Mediator.Send(
            new AddCommentCommand(reviewId, request.AuthorName, request.Text, request.IsStaff), ct);

        return CreatedAtAction(nameof(GetList), new { reviewId }, comment);
    }
}
