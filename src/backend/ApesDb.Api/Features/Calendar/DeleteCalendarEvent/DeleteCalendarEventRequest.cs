namespace ApesDb.Api.Features.Calendar.DeleteCalendarEvent;

public sealed class DeleteCalendarEventRequest
{
    public Guid EventId { get; init; }

    public string Scope { get; init; } = "event";

    public DateTimeOffset? OriginalStart { get; init; }
}
