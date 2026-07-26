using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ApesDb.Common;

public static class ApesDbTelemetry
{
    public const string ActivitySourceName = "ApesDb";
    public const string MeterName = "ApesDb";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName);
}
