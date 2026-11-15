using FluentValidation;
using MongoDB.Bson;
using Reviews.Domain.Entities;

namespace Reviews.Application.Common.Validation;

public static class ValidationRules
{
    public const int MaxPageSize = 100;
    public const int MaxSearchLength = 100;

    public static IRuleBuilderOptions<T, string?> MustBeObjectId<T>(this IRuleBuilder<T, string?> rule)
        => rule.Must(value => ObjectId.TryParse(value, out _))
            .WithMessage("'{PropertyName}' must be a valid ObjectId (24 hexadecimal characters)");

    public static IRuleBuilderOptions<T, int> MustBeRating<T>(this IRuleBuilder<T, int> rule)
        => rule.InclusiveBetween(Review.MinRating, Review.MaxRating);

    public static IRuleBuilderOptions<T, string?> MustBeReviewTitle<T>(this IRuleBuilder<T, string?> rule)
        => rule.MaximumLength(Review.MaxTitleLength);

    public static IRuleBuilderOptions<T, string> MustBeReviewText<T>(this IRuleBuilder<T, string> rule)
        => rule.NotEmpty().MaximumLength(Review.MaxTextLength);

    public static IRuleBuilderOptions<T, IReadOnlyList<string>?> MustBeShortPhrases<T>(
        this IRuleBuilder<T, IReadOnlyList<string>?> rule)
        => rule.Must(values => values is null
                || (values.Count <= Review.MaxListEntries
                    && values.All(v => !string.IsNullOrWhiteSpace(v) && v.Length <= Review.MaxListEntryLength)))
            .WithMessage(
                $"'{{PropertyName}}' must contain at most {Review.MaxListEntries} non-empty entries " +
                $"of up to {Review.MaxListEntryLength} characters");

    public static IRuleBuilderOptions<T, int> MustBeExpectedVersion<T>(this IRuleBuilder<T, int> rule)
        => rule.GreaterThan(0)
            .WithMessage("The current version must be passed in the If-Match header");
}
