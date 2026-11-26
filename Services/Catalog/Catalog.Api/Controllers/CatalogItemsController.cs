using System.Net.Mime;
using Catalog.Api.Metrics;
using Catalog.Bll.Dtos;
using Catalog.Bll.Dtos.CatalogItems;
using Catalog.Bll.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/catalog-items")]
public sealed class CatalogItemsController(ICatalogItemService items, CatalogMetrics metrics) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<CatalogItemDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<PagedResult<CatalogItemDto>>> GetList(
        [FromQuery] CatalogItemQuery query,
        [FromServices] IValidator<CatalogItemQuery> validator,
        CancellationToken ct)
    {
        if (await ValidateAsync(validator, query, ct) is { } problem)
            return problem;

        return Paged(await items.GetListAsync(query, ct));
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType<CatalogItemDetailsDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<CatalogItemDetailsDto>> GetById(long id, CancellationToken ct)
        => Ok(await items.GetByIdAsync(id, ct));

    [HttpPost]
    [ProducesResponseType<CatalogItemDetailsDto>(StatusCodes.Status201Created, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<CatalogItemDetailsDto>> Create(
        CreateCatalogItemDto input,
        [FromServices] IValidator<CreateCatalogItemDto> validator,
        CancellationToken ct)
    {
        if (await ValidateAsync(validator, input, ct) is { } problem)
            return problem;

        var created = await metrics.MeasureAsync("create", () => items.CreateAsync(input, ct));
        metrics.ItemCreated(created.ItemType);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> Update(
        long id,
        UpdateCatalogItemDto input,
        [FromServices] IValidator<UpdateCatalogItemDto> validator,
        CancellationToken ct)
    {
        if (await ValidateAsync(validator, input, ct) is { } problem)
            return problem;

        await metrics.MeasureAsync("update", () => items.UpdateAsync(id, input, ct));
        return NoContent();
    }

    [HttpPatch("{id:long}/price")]
    [ProducesResponseType<CatalogItemDetailsDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<CatalogItemDetailsDto>> ChangePrice(
        long id,
        ChangePriceDto input,
        [FromServices] IValidator<ChangePriceDto> validator,
        CancellationToken ct)
    {
        if (await ValidateAsync(validator, input, ct) is { } problem)
            return problem;

        var updated = await metrics.MeasureAsync("change_price", () => items.ChangePriceAsync(id, input, ct));
        metrics.PriceChanged(updated.ItemType);
        return Ok(updated);
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await metrics.MeasureAsync("delete", () => items.DeleteAsync(id, ct));
        metrics.ItemDeleted();
        return NoContent();
    }

    [HttpPut("{id:long}/categories/{categoryId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> AssignCategory(
        long id, int categoryId, AssignCategoryDto input, CancellationToken ct)
    {
        await items.AssignCategoryAsync(id, categoryId, input, ct);
        return NoContent();
    }

    [HttpDelete("{id:long}/categories/{categoryId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> RemoveCategory(long id, int categoryId, CancellationToken ct)
    {
        await items.RemoveCategoryAsync(id, categoryId, ct);
        return NoContent();
    }
}
