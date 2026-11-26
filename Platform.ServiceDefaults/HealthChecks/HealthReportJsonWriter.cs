using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Platform.ServiceDefaults.HealthChecks;

public static class HealthReportJsonWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsync(JsonSerializer.Serialize(ToResponse(report), SerializerOptions));
    }

    public static HealthReportResponse ToResponse(HealthReport report) => new(
        report.Status.ToString(),
        Math.Round(report.TotalDuration.TotalMilliseconds, 1),
        [.. report.Entries.Select(entry => new HealthCheckEntryResponse(
            entry.Key,
            entry.Value.Status.ToString(),
            entry.Value.Description,
            Math.Round(entry.Value.Duration.TotalMilliseconds, 1),
            [.. entry.Value.Tags],
            entry.Value.Exception?.GetType().Name,
            entry.Value.Data.Count == 0 ? null : entry.Value.Data))]);
}

public sealed record HealthReportResponse(string Status, double TotalDurationMs, IReadOnlyList<HealthCheckEntryResponse> Checks);

public sealed record HealthCheckEntryResponse(
    string Name,
    string Status,
    string? Description,
    double DurationMs,
    IReadOnlyList<string> Tags,
    string? Error,
    IReadOnlyDictionary<string, object>? Data);
