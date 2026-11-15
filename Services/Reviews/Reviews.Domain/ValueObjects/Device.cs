using Reviews.Domain.Common;

namespace Reviews.Domain.ValueObjects;

public sealed class Device : ValueObject
{
    public const int MaxLength = 100;

    public Device(string type, string? brand, string model)
    {
        Type = Guard.Required(type, "Device type", MaxLength);
        Brand = Guard.Optional(brand, "Device brand", MaxLength);
        Model = Guard.Required(model, "Device model", MaxLength);
    }

    public string Type { get; }

    public string? Brand { get; }

    public string Model { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return Brand;
        yield return Model;
    }
}
