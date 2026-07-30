using ApesDb.Domain.Entities.Calendar;

namespace ApesDb.Api.Features.Calendar;

public sealed record CalendarResourceResponse(Guid Id, string Title, string? PictureUrl, bool IsCurrentUser);

public sealed record CalendarEventResponse(
    Guid Id,
    Guid ResourceId,
    string Title,
    DateTimeOffset Start,
    DateTimeOffset End,
    bool AllDay,
    string TimeZoneId,
    CalendarRecurrenceContract? Recurrence,
    DateTimeOffset[] ExDates,
    Guid? RecurringEventId,
    DateTimeOffset? OriginalStart,
    bool ReadOnly,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public sealed record CalendarRangeResponse(CalendarResourceResponse[] Resources, CalendarEventResponse[] Events);
