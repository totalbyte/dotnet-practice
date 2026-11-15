using Catalog.Bll.Dtos.CatalogItems;
using FluentValidation;

namespace Catalog.Bll.Validation;

public sealed class UpdateCatalogItemDtoValidator : AbstractValidator<UpdateCatalogItemDto>
{
    public UpdateCatalogItemDtoValidator()
    {
        When(x => x.Details is not null, () =>
            RuleFor(x => x.Details!.Specifications).JsonObject());
    }
}
