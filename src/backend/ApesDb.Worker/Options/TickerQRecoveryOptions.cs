using System.ComponentModel.DataAnnotations;

namespace ApesDb.Worker.Options;

public sealed class TickerQRecoveryOptions
{
    public const string SectionName = "TickerQ:Recovery";

    [Range(1, 300)]
    public int HeartbeatIntervalSeconds { get; init; } = 10;
}
