using ApesDb.Domain.Entities.Calendar;

namespace ApesDb.Api.Features.Calendar.UpdateCalendarEvent;

public sealed class UpdateCalendarEventRequest : ICalendarEventMutationRequest
{
    public Guid EventId { get; init; }

    public string Scope { get; init; } = "event";

    public DateTimeOffset? OriginalStart { get; init; }

    public string Title { get; init; } = string.Empty;

    public DateTimeOffset Start { get; init; }

    public DateTimeOffset End { get; init; }

    public bool AllDay { get; init; }

    public string TimeZoneId { get; init; } = string.Empty;

    public CalendarRecurrenceContract? Recurrence { get; init; }
}
