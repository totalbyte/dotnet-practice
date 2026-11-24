using System.Text.Json;
using System.Text.Json.Serialization;
using Catalog.Grpc;
using Platform.Aggregator.Caching;
using Platform.Aggregator.Clients;
using Platform.Aggregator.ExceptionHandling;
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

builder.Services.AddGrpcClient<RepairsService.RepairsServiceClient>(o =>
    o.Address = UpstreamServices.GrpcAddress(UpstreamServices.Repairs));
builder.Services.AddGrpcClient<CatalogService.CatalogServiceClient>(o =>
    o.Address = UpstreamServices.GrpcAddress(UpstreamServices.Catalog));
builder.Services.AddGrpcClient<ReviewsService.ReviewsServiceClient>(o =>
    o.Address = UpstreamServices.GrpcAddress(UpstreamServices.Reviews));

builder.AddRedisDistributedCache("redis");
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
