using System.ComponentModel.DataAnnotations;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Dtos.Brands;

public sealed record BrandQuery : PagedQuery
{
    [StringLength(Brand.NameMaxLength)]
    public string? Search { get; init; }
}
