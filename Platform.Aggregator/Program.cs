using System.Text.Json;
using System.Text.Json.Serialization;
using Catalog.Grpc;
using Platform.Aggregator.Caching;
using Platform.Aggregator.ExceptionHandling;
using Platform.Aggregator.HealthChecks;
using Platform.Aggregator.Resilience;
using Platform.Aggregator.Services;
using Platform.ServiceDefaults.Correlation;
using Repairs.Grpc;
using Reviews.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOptions<AggregatorOptions>()
    .Bind(builder.Configuration.GetSection(AggregatorOptions.SectionName))
    .Validate(o => o.CardCacheTimeToLive > TimeSpan.Zero && o.CardCacheTimeToLive <= TimeSpan.FromSeconds(60),
        "The aggregated card is cached for at most 60 seconds, less than the caches of the services")
    .ValidateOnStart();

if (UpstreamPolicies.All.SelectMany(policy => policy.Validate()).ToList() is { Count: > 0 } policyErrors)
    throw new InvalidOperationException("Resilience policies are inconsistent: " + string.Join("; ", policyErrors));

builder.Services.AddUpstreamGrpcClient<RepairsService.RepairsServiceClient>(UpstreamPolicies.Repairs);
builder.Services.AddUpstreamGrpcClient<CatalogService.CatalogServiceClient>(UpstreamPolicies.Catalog);
builder.Services.AddUpstreamGrpcClient<ReviewsService.ReviewsServiceClient>(UpstreamPolicies.Reviews);

builder.AddRedisDistributedCache("redis",
    settings => settings.DisableHealthChecks = true,
    options =>
    {
        options.AsyncTimeout = 1000;
        options.SyncTimeout = 1000;
    });
builder.Services.AddAggregatorHealthChecks();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<AggregatorCacheMetrics>();
builder.Services.AddSingleton<RepairOrderCardCache>();
builder.Services.AddScoped<RepairOrderCardService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["correlationId"] =
        CorrelationIdMiddleware.GetCorrelationId(context.HttpContext));
builder.Services.AddExceptionHandler<UpstreamUnavailableExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
