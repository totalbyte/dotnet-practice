using System.ComponentModel.DataAnnotations;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Dtos.CatalogItems;

public sealed record UpdateCatalogItemDto
{
    [Required, StringLength(CatalogItem.NameMaxLength, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int? BrandId { get; init; }

    [Required]
    public bool? IsActive { get; init; }

    public CatalogItemDetailInputDto? Details { get; init; }

    [StringLength(CatalogItem.AuthorMaxLength)]
    public string? UpdatedBy { get; init; }
}
