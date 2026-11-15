using System.Text.Json;
using Catalog.Bll.Dtos;
using FluentValidation;

namespace Catalog.Bll.Validation;

internal static class ValidationRules
{
    public const string SkuPattern = "^[A-Z0-9]+(-[A-Z0-9]+)*$";
    public const string SlugPattern = "^[a-z0-9]+(-[a-z0-9]+)*$";

    public static IRuleBuilderOptions<T, decimal> ValidPrice<T>(this IRuleBuilder<T, decimal> rule)
        => rule.GreaterThan(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);

    public static IRuleBuilderOptions<T, string> ValidSlug<T>(this IRuleBuilder<T, string> rule)
        => rule.Matches(SlugPattern)
            .WithMessage("'{PropertyName}' must contain lowercase letters, digits and single hyphens only");

    public static IRuleBuilderOptions<T, string?> JsonObject<T>(this IRuleBuilder<T, string?> rule)
        => rule.Must(IsJsonObject).WithMessage("'{PropertyName}' must be a JSON object");

    public static IRuleBuilderOptions<T, string> SortDirection<T>(this IRuleBuilder<T, string> rule)
        => rule.Must(value => OneOf(value, [PagedQuery.Ascending, PagedQuery.Descending]))
            .WithMessage($"'{{PropertyName}}' must be '{PagedQuery.Ascending}' or '{PagedQuery.Descending}'");

    public static IRuleBuilderOptions<T, string> SortField<T>(
        this IRuleBuilder<T, string> rule, IReadOnlyList<string> allowed)
        => rule.Must(value => OneOf(value, allowed))
            .WithMessage($"'{{PropertyName}}' must be one of: {string.Join(", ", allowed)}");

    private static bool OneOf(string? value, IReadOnlyList<string> allowed)
        => allowed.Contains(value, StringComparer.OrdinalIgnoreCase);

    private static bool IsJsonObject(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        try
        {
            using var document = JsonDocument.Parse(value);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
