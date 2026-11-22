using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.ServiceDefaults.Correlation;
using Platform.Tests.Aggregator;

namespace Platform.Tests.ServiceDefaults;

public sealed class CorrelationIdTests
{
    private const string Header = CorrelationIdMiddleware.HeaderName;

    [Fact]
    public async Task Middleware_generates_id_when_request_has_none()
    {
        var (context, response) = CreateContext();
        string? seenByNext = null;

        await CreateMiddleware(ctx => seenByNext = ctx.Request.Headers[Header]).InvokeAsync(context);
        await response.StartAsync();

        Assert.True(Guid.TryParse(seenByNext, out _));
        Assert.Equal(seenByNext, context.Response.Headers[Header].ToString());
    }

    [Fact]
    public async Task Middleware_keeps_id_supplied_by_the_caller()
    {
        var (context, response) = CreateContext();
        context.Request.Headers[Header] = "order-7-check";

        await CreateMiddleware(_ => { }).InvokeAsync(context);
        await response.StartAsync();

        Assert.Equal("order-7-check", context.Request.Headers[Header].ToString());
        Assert.Equal("order-7-check", context.Response.Headers[Header].ToString());
    }

    [Theory]
    [InlineData("bad id with spaces")]
    [InlineData("<script>")]
    public async Task Middleware_replaces_invalid_id(string supplied)
    {
        var (context, _) = CreateContext();
        context.Request.Headers[Header] = supplied;

        await CreateMiddleware(_ => { }).InvokeAsync(context);

        Assert.True(Guid.TryParse(context.Request.Headers[Header], out _));
    }

    [Fact]
    public async Task Handler_forwards_id_of_the_current_request()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[Header] = "abc-123";
        var downstream = StubHttpHandler.Json(_ => new { });
        using var client = new HttpClient(new CorrelationIdHandler(new HttpContextAccessor { HttpContext = context })
        {
            InnerHandler = downstream
        });

        await client.GetAsync("http://catalog-api/api/catalog-items/1");

        Assert.Equal(["abc-123"], downstream.Requests.Single().Headers.GetValues(Header));
    }

    [Fact]
    public async Task Handler_sends_nothing_outside_of_a_request()
    {
        var downstream = StubHttpHandler.Json(_ => new { });
        using var client = new HttpClient(new CorrelationIdHandler(new HttpContextAccessor()) { InnerHandler = downstream });

        await client.GetAsync("http://catalog-api/api/catalog-items/1");

        Assert.False(downstream.Requests.Single().Headers.Contains(Header));
    }

    private static CorrelationIdMiddleware CreateMiddleware(Action<HttpContext> next) => new(
        context =>
        {
            next(context);
            return Task.CompletedTask;
        },
        NullLogger<CorrelationIdMiddleware>.Instance);

    private static (DefaultHttpContext Context, TestResponseFeature Response) CreateContext()
    {
        var context = new DefaultHttpContext();
        var response = new TestResponseFeature();
        context.Features.Set<IHttpResponseFeature>(response);
        context.Request.Path = "/api/repair-orders/1";
        return (context, response);
    }

    private sealed class TestResponseFeature : HttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _onStarting = [];

        public override void OnStarting(Func<object, Task> callback, object state) => _onStarting.Add((callback, state));

        public async Task StartAsync()
        {
            foreach (var (callback, state) in _onStarting)
                await callback(state);
        }
    }
}
