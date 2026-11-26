using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Reviews.Infrastructure.Persistence;

namespace Reviews.WebApi.HealthChecks;

public static class ReviewsHealthChecks
{
    public static IHealthChecksBuilder AddReviewsHealthChecks(this IServiceCollection services)
        => services.AddHealthChecks()
            .AddMongoDb(
                clientFactory: serviceProvider => serviceProvider.GetRequiredService<MongoDbContext>().Client,
                databaseNameFactory: serviceProvider =>
                    serviceProvider.GetRequiredService<IOptions<MongoDbOptions>>().Value.DatabaseName,
                name: "reviews-db",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["database", "mongodb", Extensions.ReadyTag],
                timeout: TimeSpan.FromSeconds(5));
}
