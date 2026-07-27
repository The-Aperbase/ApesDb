namespace ApesDb.Api.Features.Calendar.GetCalendarEvents;

public sealed class GetCalendarEventsRequest
{
    public DateTimeOffset Start { get; init; }

    public DateTimeOffset End { get; init; }
}
