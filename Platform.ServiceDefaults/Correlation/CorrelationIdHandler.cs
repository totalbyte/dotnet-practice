using Microsoft.AspNetCore.Http;

namespace Platform.ServiceDefaults.Correlation;

public sealed class CorrelationIdHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = accessor.HttpContext;
        var correlationId = context is null ? null : CorrelationIdMiddleware.GetCorrelationId(context);

        if (correlationId is not null && !request.Headers.Contains(CorrelationIdMiddleware.HeaderName))
            request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, correlationId);

        return base.SendAsync(request, cancellationToken);
    }
}
