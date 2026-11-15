using System.ComponentModel.DataAnnotations;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Dtos.Categories;

public sealed record CreateCategoryDto
{
    [Required, StringLength(Category.NameMaxLength, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Required, StringLength(Category.SlugMaxLength)]
    public string Slug { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int? ParentCategoryId { get; init; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; init; }
}
