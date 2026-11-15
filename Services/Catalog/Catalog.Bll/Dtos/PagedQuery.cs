using System.ComponentModel.DataAnnotations;

namespace Catalog.Bll.Dtos;

public abstract record PagedQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const string Ascending = "asc";
    public const string Descending = "desc";

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, int.MaxValue)]
    public int PageSize { get; init; } = DefaultPageSize;

    public string SortDir { get; init; } = Ascending;

    public int EffectivePageSize() => Math.Clamp(PageSize, 1, MaxPageSize);

    public bool IsDescending() => string.Equals(SortDir, Descending, StringComparison.OrdinalIgnoreCase);
}
