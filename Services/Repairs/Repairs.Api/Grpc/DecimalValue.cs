namespace Repairs.Grpc;

public sealed partial class DecimalValue
{
    private const decimal NanoFactor = 1_000_000_000m;

    public static DecimalValue FromDecimal(decimal value)
    {
        var units = decimal.ToInt64(decimal.Truncate(value));
        return new DecimalValue { Units = units, Nanos = decimal.ToInt32((value - units) * NanoFactor) };
    }

    public decimal ToDecimal() => Units + Nanos / NanoFactor;
}
