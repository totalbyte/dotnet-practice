using System.ComponentModel.DataAnnotations;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Dtos.CatalogItems;

public sealed record CatalogItemDetailInputDto
{
    [StringLength(CatalogItemDetail.DescriptionMaxLength)]
    public string? Description { get; init; }

    [Range(0, CatalogItemDetail.MaxWarrantyMonths)]
    public int WarrantyMonths { get; init; }

    [Range(1, int.MaxValue)]
    public int? EstimatedDurationMinutes { get; init; }

    public string? Specifications { get; init; }
}
