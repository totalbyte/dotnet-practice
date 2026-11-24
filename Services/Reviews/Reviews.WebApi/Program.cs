using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Platform.ServiceDefaults.Correlation;
using Reviews.Application;
using Reviews.Application.Common.Mappings;
using Reviews.Infrastructure;
using Reviews.Infrastructure.Persistence;
using Reviews.WebApi.ExceptionHandling;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("ReviewsDb");

builder.Services
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

if (!string.IsNullOrWhiteSpace(connectionString))
    builder.Services.PostConfigure<MongoDbOptions>(options => options.ConnectionString = connectionString);

builder.Services.AddAutoMapper(cfg =>
{
    cfg.LicenseKey = builder.Configuration[Reviews.Application.DependencyInjection.LicenseKeySetting];
    cfg.AddMaps(typeof(ReviewProfile).Assembly);
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] =
        Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

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
