using Microsoft.AspNetCore.Mvc;
using Reviews.Application.Common.Models;
using Reviews.Application.Reviews.Commands.CreateReview;
using Reviews.Application.Reviews.Commands.DeleteReview;
using Reviews.Application.Reviews.Commands.ReplyToReview;
using Reviews.Application.Reviews.Commands.UpdateReview;
using Reviews.Application.Reviews.Dtos;
using Reviews.Application.Reviews.Queries.GetReviewById;
using Reviews.Application.Reviews.Queries.GetReviewsFeed;
using Reviews.Application.Reviews.Queries.GetReviewsList;
using Reviews.WebApi.Contracts;
using Reviews.WebApi.Metrics;

namespace Reviews.WebApi.Controllers;

[Route("api/reviews")]
public sealed class ReviewsController(ReviewMetrics metrics) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<ReviewSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ReviewSummaryDto>>> GetList(
        [FromQuery] GetReviewsListQuery query, CancellationToken ct)
        => Ok(await Mediator.Send(query, ct));

    [HttpGet("feed")]
    [ProducesResponseType<CursorPage<ReviewSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CursorPage<ReviewSummaryDto>>> GetFeed(
        [FromQuery] GetReviewsFeedQuery query, CancellationToken ct)
        => Ok(await Mediator.Send(query, ct));

    [HttpGet("{id}")]
    [ProducesResponseType<ReviewDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewDetailsDto>> GetById(string id, CancellationToken ct)
    {
        var review = await Mediator.Send(new GetReviewByIdQuery(id), ct);
        SetETag(review.Version);
        return Ok(review);
    }

    [HttpPost]
    [ProducesResponseType<CreatedReviewResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreatedReviewResponse>> Create(CreateReviewCommand command, CancellationToken ct)
    {
        var id = await metrics.MeasureAsync("create", () => Mediator.Send(command, ct));
        metrics.ReviewCreated(command.ReviewType?.ToString() ?? "unknown", command.Rating);
        SetETag(1);
        return CreatedAtAction(nameof(GetById), new { id }, new CreatedReviewResponse(id));
    }

    [HttpPut("{id}")]
    [ProducesResponseType<ReviewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReviewDto>> Update(
        string id,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        UpdateReviewRequest request,
        CancellationToken ct)
    {
        var command = new UpdateReviewCommand(
            id, ParseVersion(ifMatch), request.Rating, request.Title, request.Text, request.Pros, request.Cons);

        var review = await Mediator.Send(command, ct);
        SetETag(review.Version);
        return Ok(review);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        string id, [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken ct)
    {
        await Mediator.Send(new DeleteReviewCommand(id, ParseVersion(ifMatch)), ct);
        return NoContent();
    }

    [HttpPut("{id}/staff-reply")]
    [ProducesResponseType<ReviewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReviewDto>> Reply(string id, StaffReplyRequest request, CancellationToken ct)
    {
        var review = await metrics.MeasureAsync("reply",
            () => Mediator.Send(new ReplyToReviewCommand(id, request.AuthorName, request.Text), ct));
        metrics.ReviewReplied();
        SetETag(review.Version);
        return Ok(review);
    }
}
