using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reviews.Application.Common.Interfaces;
using Reviews.Domain.Interfaces;
using Reviews.Infrastructure.Persistence;
using Reviews.Infrastructure.Seeding;

namespace Reviews.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        MongoMappings.Register();

        services.AddOptions<MongoDbOptions>()
            .Bind(configuration.GetSection(MongoDbOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString), "MongoDb:ConnectionString is required")
            .Validate(o => !string.IsNullOrWhiteSpace(o.DatabaseName), "MongoDb:DatabaseName is required")
            .ValidateOnStart();

        services.AddSingleton<MongoDbContext>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IReviewQueries, ReviewQueries>();

        services.AddScoped<IDataSeeder, ReviewSeeder>();
        services.AddHostedService<IndexCreationService>();
        services.AddHostedService<DataSeedingService>();

        return services;
    }
}
