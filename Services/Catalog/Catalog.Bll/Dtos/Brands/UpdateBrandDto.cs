using System.ComponentModel.DataAnnotations;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Dtos.Brands;

public sealed record UpdateBrandDto
{
    [Required, StringLength(Brand.NameMaxLength, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(Brand.CountryMaxLength)]
    public string? Country { get; init; }
}
