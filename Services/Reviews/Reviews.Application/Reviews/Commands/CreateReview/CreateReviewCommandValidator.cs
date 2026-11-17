using FluentValidation;
using Reviews.Application.Common.Validation;
using Reviews.Domain.Entities;
using Reviews.Domain.Enums;
using Reviews.Domain.Interfaces;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Reviews.Commands.CreateReview;

public sealed class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator(IReviewRepository reviews)
    {
        RuleFor(x => x.ReviewType).NotNull().IsInEnum();
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.AuthorName).NotEmpty().MaximumLength(Author.MaxDisplayNameLength);
        RuleFor(x => x.Rating).MustBeRating();
        RuleFor(x => x.Title).MustBeReviewTitle();
        RuleFor(x => x.Text).MustBeReviewText();

        When(x => x.ReviewType == ReviewType.Item, () =>
        {
            RuleFor(x => x.CatalogItemId).NotNull().GreaterThan(0);
            RuleFor(x => x.ItemName).NotEmpty().MaximumLength(Review.MaxItemNameLength);
            RuleFor(x => x.Pros).MustBeShortPhrases();
            RuleFor(x => x.Cons).MustBeShortPhrases();
            RuleFor(x => x.RepairOrderId).Null().WithMessage("'Repair Order Id' is allowed only for repair reviews");
            RuleFor(x => x.Device).Null().WithMessage("'Device' is allowed only for repair reviews");

            RuleFor(x => x)
                .MustAsync(async (command, ct) => !await reviews.ExistsForCatalogItemAsync(
                    command.CatalogItemId!.Value, command.CustomerId, ct))
                .When(x => x.CatalogItemId > 0 && x.CustomerId > 0)
                .OverridePropertyName(nameof(CreateReviewCommand.CatalogItemId))
                .WithMessage("The customer has already reviewed this catalog item");
        });

        When(x => x.ReviewType == ReviewType.Repair, () =>
        {
            RuleFor(x => x.RepairOrderId).NotNull().GreaterThan(0);
            RuleFor(x => x.Device).NotNull();
            RuleFor(x => x.Device!.Type).NotEmpty().MaximumLength(Device.MaxLength).When(x => x.Device is not null);
            RuleFor(x => x.Device!.Brand).MaximumLength(Device.MaxLength).When(x => x.Device is not null);
            RuleFor(x => x.Device!.Model).NotEmpty().MaximumLength(Device.MaxLength).When(x => x.Device is not null);
            RuleFor(x => x.CatalogItemId).Null().WithMessage("'Catalog Item Id' is allowed only for catalog item reviews");
            RuleFor(x => x.ItemName).Null().WithMessage("'Item Name' is allowed only for catalog item reviews");
            RuleFor(x => x.Pros).Must(p => p is null || p.Count == 0)
                .WithMessage("'Pros' are allowed only for catalog item reviews");
            RuleFor(x => x.Cons).Must(c => c is null || c.Count == 0)
                .WithMessage("'Cons' are allowed only for catalog item reviews");

            RuleFor(x => x)
                .MustAsync(async (command, ct) => !await reviews.ExistsForRepairOrderAsync(command.RepairOrderId!.Value, ct))
                .When(x => x.RepairOrderId > 0)
                .OverridePropertyName(nameof(CreateReviewCommand.RepairOrderId))
                .WithMessage("This repair order has already been reviewed");
        });
    }
}
