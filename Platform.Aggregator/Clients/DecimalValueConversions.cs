namespace Catalog.Grpc
{
    public sealed partial class DecimalValue
    {
        public decimal ToDecimal() => Units + Nanos / 1_000_000_000m;
    }
}

namespace Repairs.Grpc
{
    public sealed partial class DecimalValue
    {
        public decimal ToDecimal() => Units + Nanos / 1_000_000_000m;
    }
}
