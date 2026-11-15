using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Reviews.Infrastructure.Persistence.Serializers;

public sealed class LowerCaseEnumSerializer<TEnum> : StructSerializerBase<TEnum>
    where TEnum : struct, Enum
{
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TEnum value)
        => context.Writer.WriteString(value.ToString().ToLowerInvariant());

    public override TEnum Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var value = context.Reader.ReadString();
        return Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) && Enum.IsDefined(result)
            ? result
            : throw new FormatException($"'{value}' is not a valid {typeof(TEnum).Name} value");
    }
}
