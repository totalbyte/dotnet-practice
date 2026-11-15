using System.ComponentModel.DataAnnotations;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Dtos.CatalogItems;

public sealed record CreateCatalogItemDto
{
    [Required, StringLength(CatalogItem.SkuMaxLength)]
    public string Sku { get; init; } = string.Empty;

    [Required, StringLength(CatalogItem.NameMaxLength, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Required]
    public CatalogItemType? ItemType { get; init; }

    public decimal Price { get; init; }

    [Range(1, int.MaxValue)]
    public int? BrandId { get; init; }

    public CatalogItemDetailInputDto? Details { get; init; }

    [StringLength(CatalogItem.AuthorMaxLength)]
    public string? CreatedBy { get; init; }
}
