using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Reviews.WebApi.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected void SetETag(int version)
        => Response.Headers.ETag = new EntityTagHeaderValue($"\"{version}\"").ToString();

    protected static int ParseVersion(string? ifMatch)
    {
        if (string.IsNullOrWhiteSpace(ifMatch) || !EntityTagHeaderValue.TryParse(ifMatch, out var tag))
            return 0;

        return int.TryParse(tag.Tag.ToString().Trim('"'), out var version) ? version : 0;
    }
}
