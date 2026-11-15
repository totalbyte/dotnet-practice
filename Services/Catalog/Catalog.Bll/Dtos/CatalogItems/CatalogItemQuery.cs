using System.ComponentModel.DataAnnotations;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Dtos.CatalogItems;

public sealed record CatalogItemQuery : PagedQuery
{
    public const string SortByName = "name";
    public const string SortByPrice = "price";
    public const string SortBySku = "sku";
    public const string SortByCreatedAt = "createdat";

    public static readonly IReadOnlyList<string> SortFields =
        [SortByName, SortByPrice, SortBySku, SortByCreatedAt];

    [StringLength(CatalogItem.NameMaxLength)]
    public string? Search { get; init; }

    public CatalogItemType? ItemType { get; init; }

    [Range(1, int.MaxValue)]
    public int? BrandId { get; init; }

    [Range(1, int.MaxValue)]
    public int? CategoryId { get; init; }

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }

    public bool? IsActive { get; init; }

    public string SortBy { get; init; } = SortByName;
}
