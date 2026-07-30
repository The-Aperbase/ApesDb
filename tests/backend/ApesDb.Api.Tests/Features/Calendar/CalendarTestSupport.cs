using System.Net.Http.Json;
using ApesDb.Api.Features.Calendar;
using ApesDb.Api.Tests.Infrastructure.Http;

namespace ApesDb.Api.Tests.Features.Calendar;

internal static class CalendarTestSupport
{
    public static string EventsUrl(DateTimeOffset start, DateTimeOffset end)
    {
        return $"/api/calendar/events?start={Uri.EscapeDataString(start.ToString("O"))}"
            + $"&end={Uri.EscapeDataString(end.ToString("O"))}";
    }

    public static async Task<CalendarEventResponse> ReadEventAsync(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<CalendarEventResponse>()
            ?? throw new InvalidOperationException("The calendar event response was empty.");
    }

    public static async Task<HttpResponseSnapshot> RangeSnapshotAsync(HttpResponseMessage response)
    {
        return await HttpResponseSnapshot.CreateAsync<CalendarRangeResponse>(response);
    }
}
