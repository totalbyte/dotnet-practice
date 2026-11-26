namespace Reviews.WebApi.Metrics;

public static class ReviewMetricsRegistration
{
    public static IServiceCollection AddReviewMetrics(this IServiceCollection services)
        => services
            .AddSingleton<ReviewMetrics>()
            .AddHostedService<PublishedReviewsSnapshot>();
}
