using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Repairs.Bll.Dtos.CatalogItems;
using Repairs.Bll.Interfaces;

namespace Repairs.Api.Controllers;

[ApiController]
[Route("api/catalog-items")]
public sealed class CatalogItemsController(ICatalogItemService catalogItems) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CatalogItemDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public async Task<ActionResult<IReadOnlyList<CatalogItemDto>>> GetAll(CancellationToken ct)
        => Ok(await catalogItems.GetAllAsync(ct));

    [HttpGet("{id:long}")]
    [ProducesResponseType<CatalogItemDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<CatalogItemDto>> GetById(long id, CancellationToken ct)
        => Ok(await catalogItems.GetByIdAsync(id, ct));

    [HttpPut("{id:long}/stock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> UpdateStock(long id, UpdateStockDto input, CancellationToken ct)
    {
        await catalogItems.UpdateStockAsync(id, input, ct);
        return NoContent();
    }
}
