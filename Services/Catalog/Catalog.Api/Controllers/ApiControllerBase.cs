using System.Text.Json;
using Catalog.Bll.Dtos;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    public const string PaginationHeader = "X-Pagination";

    private static readonly JsonSerializerOptions HeaderJson = new(JsonSerializerDefaults.Web);

    protected async Task<ActionResult?> ValidateAsync<T>(IValidator<T> validator, T input, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(input, ct);
        if (result.IsValid)
            return null;

        foreach (var failure in result.Errors)
            ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);

        ValidationProblemDetails problem = ProblemDetailsFactory.CreateValidationProblemDetails(
            HttpContext, ModelState, StatusCodes.Status400BadRequest);
        return ValidationProblem(problem);
    }

    protected ActionResult<PagedResult<T>> Paged<T>(PagedResult<T> result)
    {
        Response.Headers[PaginationHeader] = JsonSerializer.Serialize(
            new
            {
                result.TotalCount,
                result.Page,
                result.PageSize,
                result.TotalPages,
                result.HasNext,
                result.HasPrevious
            },
            HeaderJson);

        return Ok(result);
    }
}
