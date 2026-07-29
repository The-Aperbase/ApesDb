using ApesDb.Domain.Entities.Calendar;

namespace ApesDb.Api.Features.Calendar.CreateCalendarEvent;

public sealed class CreateCalendarEventRequest : ICalendarEventMutationRequest
{
    public string Title { get; init; } = string.Empty;

    public DateTimeOffset Start { get; init; }

    public DateTimeOffset End { get; init; }

    public bool AllDay { get; init; }

    public string TimeZoneId { get; init; } = string.Empty;

    public CalendarRecurrenceContract? Recurrence { get; init; }
}
