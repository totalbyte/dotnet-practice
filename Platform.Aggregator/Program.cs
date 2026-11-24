using System.Text.Json;
using System.Text.Json.Serialization;
using Platform.Aggregator.Clients;
using Platform.Aggregator.ExceptionHandling;
using Platform.Aggregator.Services;
using Platform.ServiceDefaults.Correlation;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient<RepairsClient>(client => client.BaseAddress = new("https+http://repairs-api"));
builder.Services.AddHttpClient<CatalogClient>(client => client.BaseAddress = new("https+http://catalog-api"));
builder.Services.AddHttpClient<ReviewsClient>(client => client.BaseAddress = new("https+http://reviews-api"));

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
