using System.ComponentModel.DataAnnotations;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Dtos.CatalogItems;

public sealed record ChangePriceDto
{
    public decimal Price { get; init; }

    [StringLength(CatalogItem.AuthorMaxLength)]
    public string? UpdatedBy { get; init; }
}
