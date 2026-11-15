namespace Catalog.Bll.Dtos.Categories;

public sealed record CategoryDto(int Id, string Name, string Slug, int? ParentCategoryId, int DisplayOrder);
