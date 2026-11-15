using Catalog.Bll.Dtos.CatalogItems;
using Catalog.Domain.Entities;
using FluentValidation;

namespace Catalog.Bll.Validation;

public sealed class CreateCatalogItemDtoValidator : AbstractValidator<CreateCatalogItemDto>
{
    public CreateCatalogItemDtoValidator()
    {
        RuleFor(x => x.Sku)
            .Matches(ValidationRules.SkuPattern)
            .WithMessage("'{PropertyName}' must contain uppercase letters, digits and single hyphens only");

        RuleFor(x => x.ItemType).IsInEnum();
        RuleFor(x => x.Price).ValidPrice();

        RuleFor(x => x.BrandId)
            .Null()
            .When(x => x.ItemType == CatalogItemType.Service)
            .WithMessage("Service cannot have a brand");

        When(x => x.Details is not null, () =>
        {
            RuleFor(x => x.Details!.EstimatedDurationMinutes)
                .Null()
                .When(x => x.ItemType == CatalogItemType.Part)
                .WithMessage("Estimated duration applies to services only");

            RuleFor(x => x.Details!.Specifications).JsonObject();
        });
    }
}
