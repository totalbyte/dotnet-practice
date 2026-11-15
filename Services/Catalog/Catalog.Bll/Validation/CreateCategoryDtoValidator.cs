using Catalog.Bll.Dtos.Categories;
using FluentValidation;

namespace Catalog.Bll.Validation;

public sealed class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator()
    {
        RuleFor(x => x.Slug).ValidSlug();
    }
}
