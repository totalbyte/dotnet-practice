using Catalog.Bll.Dtos.CatalogItems;
using FluentValidation;

namespace Catalog.Bll.Validation;

public sealed class ChangePriceDtoValidator : AbstractValidator<ChangePriceDto>
{
    public ChangePriceDtoValidator()
    {
        RuleFor(x => x.Price).ValidPrice();
    }
}
