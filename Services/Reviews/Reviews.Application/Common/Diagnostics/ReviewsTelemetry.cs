using System.Diagnostics;

namespace Reviews.Application.Common.Diagnostics;

public static class ReviewsTelemetry
{
    public const string ActivitySourceName = "Reviews.Application";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
