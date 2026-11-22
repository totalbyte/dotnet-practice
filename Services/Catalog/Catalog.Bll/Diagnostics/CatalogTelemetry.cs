using System.Diagnostics;

namespace Catalog.Bll.Diagnostics;

public static class CatalogTelemetry
{
    public const string ActivitySourceName = "Catalog.Bll";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
