using System.Net.Mime;
using Catalog.Bll.Dtos;
using Catalog.Bll.Dtos.CatalogItems;
using Catalog.Bll.Dtos.Categories;
using Catalog.Bll.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(ICategoryService categories) : ApiControllerBase
{
    private const int RecentItemsDefaultDays = 30;

    [HttpGet]
    [ProducesResponseType<PagedResult<CategoryDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<PagedResult<CategoryDto>>> GetList(
        [FromQuery] CategoryQuery query,
        [FromServices] IValidator<CategoryQuery> validator,
        CancellationToken ct)
    {
        if (await ValidateAsync(validator, query, ct) is { } problem)
            return problem;

        return Paged(await categories.GetListAsync(query, ct));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<CategoryDto>> GetById(int id, CancellationToken ct)
        => Ok(await categories.GetByIdAsync(id, ct));

    [HttpGet("{id:int}/items")]
    [ProducesResponseType<CategoryWithItemsDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<CategoryWithItemsDto>> GetWithItems(int id, CancellationToken ct)
        => Ok(await categories.GetWithItemsAsync(id, ct));

    [HttpGet("{slug}/recent-items")]
    [ProducesResponseType<IReadOnlyList<CatalogItemSummaryDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<IReadOnlyList<CatalogItemSummaryDto>>> GetRecentItems(
        string slug, [FromQuery] DateTime? since, CancellationToken ct)
        => Ok(await categories.GetRecentItemsAsync(
            slug, since ?? DateTime.UtcNow.AddDays(-RecentItemsDefaultDays), ct));

    [HttpPost]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status201Created, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<CategoryDto>> Create(
        CreateCategoryDto input,
        [FromServices] IValidator<CreateCategoryDto> validator,
        CancellationToken ct)
    {
        if (await ValidateAsync(validator, input, ct) is { } problem)
            return problem;

        var created = await categories.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> Update(
        int id,
        UpdateCategoryDto input,
        [FromServices] IValidator<UpdateCategoryDto> validator,
        CancellationToken ct)
    {
        if (await ValidateAsync(validator, input, ct) is { } problem)
            return problem;

        await categories.UpdateAsync(id, input, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await categories.DeleteAsync(id, ct);
        return NoContent();
    }
}
