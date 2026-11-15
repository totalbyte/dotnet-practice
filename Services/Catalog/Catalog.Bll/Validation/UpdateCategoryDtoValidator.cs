using Catalog.Bll.Dtos.Categories;
using FluentValidation;

namespace Catalog.Bll.Validation;

public sealed class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryDtoValidator()
    {
        RuleFor(x => x.Slug).ValidSlug();
    }
}
