using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Platform.Tests.ServiceDefaults;

public sealed class HealthEndpointsTests
{
    [Fact]
    public async Task Liveness_ignores_external_dependencies()
    {
        await using var app = await StartAsync(database: HealthStatus.Unhealthy, cache: HealthStatus.Healthy,
            environment: Environments.Production);

        var response = await app.GetTestClient().GetAsync(Extensions.LivenessEndpointPath);
        var report = await ReadAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("self", Assert.Single(report.GetProperty("checks").EnumerateArray()).GetProperty("name").GetString());
    }

    [Fact]
    public async Task Readiness_fails_when_a_required_dependency_is_unhealthy()
    {
        await using var app = await StartAsync(database: HealthStatus.Unhealthy, cache: HealthStatus.Healthy);

        var response = await app.GetTestClient().GetAsync(Extensions.ReadinessEndpointPath);
        var report = await ReadAsync(response);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", report.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Readiness_reports_degraded_cache_but_keeps_the_service_in_rotation()
    {
        await using var app = await StartAsync(database: HealthStatus.Healthy, cache: HealthStatus.Degraded);

        var response = await app.GetTestClient().GetAsync(Extensions.ReadinessEndpointPath);
        var report = await ReadAsync(response);
        var checks = report.GetProperty("checks").EnumerateArray().ToDictionary(
            check => check.GetProperty("name").GetString()!, check => check);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Degraded", report.GetProperty("status").GetString());
        Assert.Equal(["db", "redis-cache"], checks.Keys.Order());
        Assert.Equal("Degraded", checks["redis-cache"].GetProperty("status").GetString());
        Assert.Contains("ready", checks["db"].GetProperty("tags").EnumerateArray().Select(tag => tag.GetString()));
        Assert.Equal("TimeoutException", checks["redis-cache"].GetProperty("error").GetString());
        Assert.DoesNotContain("secret", await response.Content.ReadAsStringAsync());
    }

    private static async Task<WebApplication> StartAsync(
        HealthStatus database, HealthStatus cache, string environment = "Development")
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseTestServer();
        builder.AddDefaultHealthChecks();
        builder.Services.AddHealthChecks()
            .AddCheck("db", () => new HealthCheckResult(database), [Extensions.ReadyTag])
            .AddCheck("redis-cache", () => new HealthCheckResult(cache, "cache",
                cache == HealthStatus.Healthy ? null : new TimeoutException("secret connection details")), [Extensions.ReadyTag]);

        var app = builder.Build();
        app.MapDefaultEndpoints();
        await app.StartAsync();
        return app;
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
        => JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
}
