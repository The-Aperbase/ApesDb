namespace ApesDb.Domain.Entities.Calendar;

public sealed class CalendarRecurrenceContract
{
    public string Frequency { get; init; } = string.Empty;

    public int Interval { get; init; } = 1;

    public int? Count { get; init; }

    public DateTimeOffset? Until { get; init; }

    public string[] ByWeekday { get; init; } = [];

    public int[] ByMonthDay { get; init; } = [];

    public int[] ByMonth { get; init; } = [];

    public string? WeekStart { get; init; }
}
