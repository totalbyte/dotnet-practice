using System.Diagnostics;

namespace Repairs.Bll.Diagnostics;

public static class RepairsTelemetry
{
    public const string ActivitySourceName = "Repairs.Bll";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
