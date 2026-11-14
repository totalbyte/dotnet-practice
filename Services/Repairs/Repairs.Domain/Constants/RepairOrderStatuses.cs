namespace Repairs.Domain.Constants;

public static class RepairOrderStatuses
{
    public const string Accepted = "Accepted";
    public const string Diagnosed = "Diagnosed";
    public const string Approved = "Approved";
    public const string InRepair = "InRepair";
    public const string Ready = "Ready";
    public const string Paid = "Paid";
    public const string Issued = "Issued";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlySet<string> ItemsEditable =
        new HashSet<string> { Accepted, Diagnosed, InRepair };

    public static readonly IReadOnlySet<string> Closed =
        new HashSet<string> { Issued, Cancelled };
}
