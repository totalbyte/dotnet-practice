using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Repairs.Api.Metrics;
using Repairs.Bll.Dtos;
using Repairs.Bll.Dtos.RepairOrders;
using Repairs.Bll.Interfaces;

namespace Repairs.Api.Controllers;

[ApiController]
[Route("api/repair-orders")]
public sealed class RepairOrdersController(IRepairOrderService repairOrders, RepairOrderMetrics metrics) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResultDto<RepairOrderSummaryDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<PagedResultDto<RepairOrderSummaryDto>>> Search(
        [FromQuery] RepairOrderQueryDto query, CancellationToken ct)
        => Ok(await repairOrders.SearchAsync(query, ct));

    [HttpGet("{id:long}")]
    [ProducesResponseType<RepairOrderDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<RepairOrderDto>> GetById(long id, CancellationToken ct)
        => Ok(await repairOrders.GetByIdAsync(id, ct));

    [HttpPost]
    [ProducesResponseType<RepairOrderDto>(StatusCodes.Status201Created, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<RepairOrderDto>> Create(CreateRepairOrderDto input, CancellationToken ct)
    {
        var created = await metrics.MeasureAsync("create", () => repairOrders.CreateAsync(input, ct));
        metrics.OrderCreated(input.DeviceType);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("{id:long}/items")]
    [ProducesResponseType<RepairOrderDto>(StatusCodes.Status201Created, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<RepairOrderDto>> AddItem(
        long id, AddRepairOrderItemDto input, CancellationToken ct)
    {
        var updated = await metrics.MeasureAsync("add_item", () => repairOrders.AddItemAsync(id, input, ct));
        metrics.ItemAdded();
        return CreatedAtAction(nameof(GetById), new { id }, updated);
    }

    [HttpPost("{id:long}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> ChangeStatus(
        long id, ChangeRepairOrderStatusDto input, CancellationToken ct)
    {
        await metrics.MeasureAsync("change_status", () => repairOrders.ChangeStatusAsync(id, input, ct));
        metrics.StatusChanged(input.Status);
        return NoContent();
    }

    [HttpPut("{id:long}/details")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> UpdateDetails(
        long id, UpdateRepairOrderDetailsDto input, CancellationToken ct)
    {
        await repairOrders.UpdateDetailsAsync(id, input, ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await repairOrders.DeleteAsync(id, ct);
        return NoContent();
    }
}
