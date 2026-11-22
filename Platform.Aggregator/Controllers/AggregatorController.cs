using Microsoft.AspNetCore.Mvc;
using Platform.Aggregator.Dtos;
using Platform.Aggregator.Services;

namespace Platform.Aggregator.Controllers;

[ApiController]
[Route("api/aggregator")]
[Produces("application/json")]
public sealed class AggregatorController(RepairOrderCardService cards) : ControllerBase
{
    [HttpGet("repair-orders/{id:long}/full")]
    [ProducesResponseType<RepairOrderCardDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<RepairOrderCardDto>> GetRepairOrderCard(long id, CancellationToken cancellationToken)
    {
        var card = await cards.GetAsync(id, cancellationToken);

        return card is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Repair order not found",
                detail: $"Repair order {id} not found")
            : Ok(card);
    }
}
