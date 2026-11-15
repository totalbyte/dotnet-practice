using System.ComponentModel.DataAnnotations;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Dtos.Categories;

public sealed record CategoryQuery : PagedQuery
{
    public const string SortByName = "name";
    public const string SortByDisplayOrder = "displayorder";

    public static readonly IReadOnlyList<string> SortFields = [SortByName, SortByDisplayOrder];

    [StringLength(Category.NameMaxLength)]
    public string? Search { get; init; }

    [Range(1, int.MaxValue)]
    public int? ParentCategoryId { get; init; }

    public string SortBy { get; init; } = SortByName;
}
