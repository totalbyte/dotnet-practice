namespace Catalog.Bll.Dtos.Categories;

public sealed record CategoryWithItemsDto(
    int Id, string Name, string Slug, int? ParentCategoryId, IReadOnlyList<CategoryItemDto> Items);
