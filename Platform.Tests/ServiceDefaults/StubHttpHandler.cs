using System.Net;
using System.Net.Http.Json;

namespace Platform.Tests.ServiceDefaults;

internal sealed class StubHttpHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
    : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    public static StubHttpHandler Json(Func<HttpRequestMessage, object?> body) => new((request, _) =>
    {
        var content = body(request);
        return Task.FromResult(content is null
            ? new HttpResponseMessage(HttpStatusCode.NotFound)
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(content) });
    });

    public static StubHttpHandler Failing(Exception exception) => new((_, _) => Task.FromException<HttpResponseMessage>(exception));

    public Task<HttpResponseMessage> InvokeAsync(HttpRequestMessage request) => SendAsync(request, CancellationToken.None);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        lock (Requests)
            Requests.Add(request);
        return respond(request, cancellationToken);
    }
}
