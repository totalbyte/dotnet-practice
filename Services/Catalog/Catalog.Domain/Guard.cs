using Catalog.Domain.Exceptions;

namespace Catalog.Domain;

internal static class Guard
{
    public static string Required(string? value, int maxLength, string name)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new DomainException($"{name} is required");
        if (trimmed.Length > maxLength)
            throw new DomainException($"{name} must not exceed {maxLength} characters");
        return trimmed;
    }

    public static string? Optional(string? value, int maxLength, string name)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;
        if (trimmed.Length > maxLength)
            throw new DomainException($"{name} must not exceed {maxLength} characters");
        return trimmed;
    }
}
