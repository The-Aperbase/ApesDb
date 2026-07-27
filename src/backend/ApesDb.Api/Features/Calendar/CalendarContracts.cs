using System.Text.Json;
using ApesDb.Domain.Entities.Calendar;

namespace ApesDb.Api.Features.Calendar;

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

public static class CalendarContractFactory
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static string? SerializeRecurrence(CalendarRecurrenceContract? recurrence)
    {
        if (recurrence is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(recurrence, SerializerOptions);
    }

    public static CalendarRecurrenceContract? DeserializeRecurrence(string? recurrenceJson)
    {
        if (recurrenceJson is null)
        {
            return null;
        }

        return JsonSerializer.Deserialize<CalendarRecurrenceContract>(recurrenceJson, SerializerOptions);
    }

    public static CalendarEventResponse CreateEventResponse(
        CalendarEvent calendarEvent,
        DateTimeOffset[] exDates,
        bool readOnly
    )
    {
        return new CalendarEventResponse(
            calendarEvent.Id,
            calendarEvent.OwnerUserId,
            calendarEvent.Title,
            calendarEvent.StartAt,
            calendarEvent.EndAt,
            calendarEvent.AllDay,
            calendarEvent.TimeZoneId,
            DeserializeRecurrence(calendarEvent.RecurrenceJson),
            exDates,
            calendarEvent.RecurringEventId,
            calendarEvent.OriginalStartAt,
            readOnly,
            calendarEvent.CreatedAt,
            calendarEvent.UpdatedAt
        );
    }
}
