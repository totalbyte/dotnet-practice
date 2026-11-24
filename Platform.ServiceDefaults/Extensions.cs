using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Platform.ServiceDefaults.Correlation;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Microsoft.Extensions.Hosting;

public static class Extensions
{
    public const string HealthEndpointPath = "/health";
    public const string AlivenessEndpointPath = "/alive";
    public const string CacheMeterSuffix = ".Cache";

    private const string ResilienceSection = "HttpClientResilience";
    private const string MongoDbActivitySource = "MongoDB.Driver.Core.Extensions.DiagnosticSources";
    private const string SamplingRatioSetting = "OpenTelemetry:TraceSamplingRatio";
    private const double DefaultProductionSamplingRatio = 0.25;

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureSerilog();
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<CorrelationIdHandler>();

        var resilience = builder.Configuration.GetSection(ResilienceSection);

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            var pipeline = http.AddStandardResilienceHandler();
            if (resilience.Exists())
                pipeline.Configure(resilience);

            http.AddServiceDiscovery();
            http.AddHttpMessageHandler<CorrelationIdHandler>();
        });

        return builder;
    }

    public static TBuilder ConfigureSerilog<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var serviceName = builder.Environment.ApplicationName;
        var minimumLevel = builder.Environment.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Information;

        // Serilog replaces the default providers; only the OpenTelemetry provider added below receives its events.
        builder.Logging.ClearProviders();

        builder.Services.AddSerilog((_, configuration) => configuration
            .ReadFrom.Configuration(builder.Configuration)
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Extensions.Diagnostics.HealthChecks", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Extensions.Http", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.Extensions.ServiceDiscovery", LogEventLevel.Information)
            .MinimumLevel.Override("Polly", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ServiceName", serviceName)
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .WriteTo.Console(new CompactJsonFormatter()),
            writeToProviders: true);

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var applicationName = builder.Environment.ApplicationName;
        var servicePrefix = applicationName.Split('.')[0];
        var serviceSources = servicePrefix + ".*";
        // Cache meters follow the "<Service>.Cache" convention: Catalog.Cache in catalog-api and Platform.Cache
        // in the aggregator; they publish cache.hits / cache.misses tagged by cache.level (L1 / L2).
        var cacheMeter = servicePrefix + CacheMeterSuffix;
        var sampler = builder.Environment.IsDevelopment()
            ? (Sampler)new AlwaysOnSampler()
            : new ParentBasedSampler(new TraceIdRatioBasedSampler(
                builder.Configuration.GetValue(SamplingRatioSetting, DefaultProductionSamplingRatio)));

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddAttributes(
            [
                new KeyValuePair<string, object>("deployment.environment.name", builder.Environment.EnvironmentName)
            ]))
            .WithMetrics(metrics => metrics
                .AddMeter(applicationName)
                .AddMeter(cacheMeter)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithTracing(tracing => tracing
                .SetSampler(sampler)
                .AddSource(applicationName)
                .AddSource(serviceSources)
                .AddSource(MongoDbActivitySource)
                .AddAspNetCoreInstrumentation(options => options.Filter = context =>
                    !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                    && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath))
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation());

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
            builder.Services.AddOpenTelemetry().UseOtlpExporter();

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Health endpoints expose dependency state, so they are published only in Development.
        if (app.Environment.IsDevelopment())
        {
            app.MapHealthChecks(HealthEndpointPath);
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("live")
            });
        }

        return app;
    }
}
