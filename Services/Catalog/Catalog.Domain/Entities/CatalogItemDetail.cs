using System.Text.Json;
using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

public class CatalogItemDetail
{
    public const int DescriptionMaxLength = 2000;
    public const int MaxWarrantyMonths = 60;

    private CatalogItemDetail()
    {
    }

    internal CatalogItemDetail(
        string? description, int warrantyMonths, int? estimatedDurationMinutes, string? specifications)
    {
        Change(description, warrantyMonths, estimatedDurationMinutes, specifications);
    }

    public long CatalogItemId { get; private set; }
    public CatalogItem CatalogItem { get; private set; } = null!;
    public string? Description { get; private set; }
    public int WarrantyMonths { get; private set; }
    public int? EstimatedDurationMinutes { get; private set; }
    public string? Specifications { get; private set; }

    internal void Change(
        string? description, int warrantyMonths, int? estimatedDurationMinutes, string? specifications)
    {
        if (warrantyMonths is < 0 or > MaxWarrantyMonths)
            throw new DomainException($"Warranty must be between 0 and {MaxWarrantyMonths} months");
        if (estimatedDurationMinutes <= 0)
            throw new DomainException("Estimated duration must be positive");

        Description = Guard.Optional(description, DescriptionMaxLength, "Description");
        WarrantyMonths = warrantyMonths;
        EstimatedDurationMinutes = estimatedDurationMinutes;
        Specifications = ValidSpecifications(specifications);
    }

    private static string? ValidSpecifications(string? specifications)
    {
        if (string.IsNullOrWhiteSpace(specifications))
            return null;

        try
        {
            using var document = JsonDocument.Parse(specifications);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
                return specifications.Trim();
        }
        catch (JsonException)
        {
        }

        throw new DomainException("Specifications must be a JSON object");
    }
}
