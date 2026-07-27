using System.Net.Http.Json;
using ApesDb.Api.Features.Calendar;
using ApesDb.Api.Features.Calendar.CreateCalendarEvent;
using ApesDb.Api.Features.Calendar.UpdateCalendarEvent;
using ApesDb.Api.Tests.Infrastructure.Authentication;
using ApesDb.Api.Tests.Infrastructure.Factories;
using ApesDb.Api.Tests.Infrastructure.Http;
using ApesDb.Api.Tests.TestData;

namespace ApesDb.Api.Tests.Features.Calendar;

public sealed class CalendarEventMutationTests : IClassFixture<MutableEndpointApiFactory>, IAsyncLifetime
{
    private static readonly DateTimeOffset RangeStart = new(2026, 3, 20, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset RangeEnd = new(2026, 4, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly MutableEndpointApiFactory _factory;

    public CalendarEventMutationTests(MutableEndpointApiFactory factory)
    {
        _factory = factory;
    }

    public async ValueTask InitializeAsync()
    {
        await _factory.ResetAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task UserCanCreateOffsetCalendarEvent()
    {
        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var content = JsonContent.Create(
            new CreateCalendarEventRequest
            {
                Title = "  Late shift  ",
                Start = new DateTimeOffset(2026, 3, 27, 22, 0, 0, TimeSpan.FromHours(1)),
                End = new DateTimeOffset(2026, 3, 28, 6, 0, 0, TimeSpan.FromHours(1)),
                TimeZoneId = "Europe/Oslo",
            }
        );
        using var createResponse = await client.PostAsync(
            "/api/calendar/events",
            content,
            TestContext.Current.CancellationToken
        );
        var created = await CalendarTestSupport.ReadEventAsync(createResponse);
        var createSnapshot = await HttpResponseSnapshot.CreateAsync<CalendarEventResponse>(createResponse);

        using var rangeResponse = await client.GetAsync(
            CalendarTestSupport.EventsUrl(RangeStart, RangeEnd),
            TestContext.Current.CancellationToken
        );
        var rangeSnapshot = await CalendarTestSupport.RangeSnapshotAsync(rangeResponse);

        await Verify(
            new
            {
                CreatedEventId = created.Id,
                CreateResponse = createSnapshot,
                RangeResponse = rangeSnapshot,
            }
        );
    }

    [Fact]
    public async Task RecurringOccurrenceCanBeChangedAndCancelled()
    {
        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        var recurrence = new CalendarRecurrenceContract
        {
            Frequency = "weekly",
            Interval = 1,
            Count = 4,
            ByWeekday = ["MO"],
            WeekStart = "MO",
        };
        var seriesStart = new DateTimeOffset(2026, 3, 23, 8, 0, 0, TimeSpan.FromHours(1));
        using var createContent = JsonContent.Create(
            new CreateCalendarEventRequest
            {
                Title = "Rota",
                Start = seriesStart,
                End = seriesStart.AddHours(8),
                TimeZoneId = "Europe/Oslo",
                Recurrence = recurrence,
            }
        );
        using var createResponse = await client.PostAsync(
            "/api/calendar/events",
            createContent,
            TestContext.Current.CancellationToken
        );
        var created = await CalendarTestSupport.ReadEventAsync(createResponse);
        var originalStart = new DateTimeOffset(2026, 3, 30, 8, 0, 0, TimeSpan.FromHours(2));
        using var updateContent = JsonContent.Create(
            new UpdateCalendarEventRequest
            {
                Scope = "occurrence",
                OriginalStart = originalStart,
                Title = "Swapped shift",
                Start = originalStart.AddHours(2),
                End = originalStart.AddHours(10),
                TimeZoneId = "Europe/Oslo",
            }
        );
        using var updateResponse = await client.PutAsync(
            $"/api/calendar/events/{created.Id}",
            updateContent,
            TestContext.Current.CancellationToken
        );
        var updateSnapshot = await HttpResponseSnapshot.CreateAsync<CalendarEventResponse>(updateResponse);

        using var changedRangeResponse = await client.GetAsync(
            CalendarTestSupport.EventsUrl(RangeStart, RangeEnd),
            TestContext.Current.CancellationToken
        );
        var changedRange = await CalendarTestSupport.RangeSnapshotAsync(changedRangeResponse);

        using var deleteResponse = await client.DeleteAsync(
            $"/api/calendar/events/{created.Id}?scope=occurrence"
                + $"&originalStart={Uri.EscapeDataString(originalStart.ToString("O"))}",
            TestContext.Current.CancellationToken
        );
        using var cancelledRangeResponse = await client.GetAsync(
            CalendarTestSupport.EventsUrl(RangeStart, RangeEnd),
            TestContext.Current.CancellationToken
        );
        var cancelledRange = await CalendarTestSupport.RangeSnapshotAsync(cancelledRangeResponse);

        await Verify(
            new
            {
                UpdateResponse = updateSnapshot,
                ChangedRange = changedRange,
                DeleteResponse = HttpResponseSnapshot.CreateWithoutContent(deleteResponse),
                CancelledRange = cancelledRange,
            }
        );
    }

    [Fact]
    public async Task UserCannotChangeAnotherUsersCalendarEvent()
    {
        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var content = JsonContent.Create(
            new UpdateCalendarEventRequest
            {
                Title = "Changed",
                Start = new DateTimeOffset(2026, 1, 15, 18, 0, 0, TimeSpan.Zero),
                End = new DateTimeOffset(2026, 1, 15, 22, 0, 0, TimeSpan.Zero),
                TimeZoneId = "Europe/London",
            }
        );
        using var updateResponse = await client.PutAsync(
            $"/api/calendar/events/{CalendarTestData.MemberEventId}",
            content,
            TestContext.Current.CancellationToken
        );

        using var rangeResponse = await client.GetAsync(
            CalendarTestSupport.EventsUrl(
                new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 1, 16, 0, 0, 0, TimeSpan.Zero)
            ),
            TestContext.Current.CancellationToken
        );

        await Verify(
            new
            {
                UpdateResponse = await HttpResponseSnapshot.CreateAsync(updateResponse),
                RangeResponse = await CalendarTestSupport.RangeSnapshotAsync(rangeResponse),
            }
        );
    }

    [Fact]
    public async Task InvalidAllDayAndRecurrenceBoundariesAreRejectedWithoutChangingCalendar()
    {
        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var invalidAllDayContent = JsonContent.Create(
            new CreateCalendarEventRequest
            {
                Title = "Misaligned all day",
                Start = new DateTimeOffset(2026, 3, 27, 1, 0, 0, TimeSpan.Zero),
                End = new DateTimeOffset(2026, 3, 28, 1, 0, 0, TimeSpan.Zero),
                AllDay = true,
                TimeZoneId = "Europe/Oslo",
            }
        );
        using var invalidAllDayResponse = await client.PostAsync(
            "/api/calendar/events",
            invalidAllDayContent,
            TestContext.Current.CancellationToken
        );

        using var invalidRecurrenceContent = JsonContent.Create(
            new CreateCalendarEventRequest
            {
                Title = "Backwards recurrence",
                Start = new DateTimeOffset(2026, 3, 27, 8, 0, 0, TimeSpan.FromHours(1)),
                End = new DateTimeOffset(2026, 3, 27, 16, 0, 0, TimeSpan.FromHours(1)),
                TimeZoneId = "Europe/Oslo",
                Recurrence = new CalendarRecurrenceContract
                {
                    Frequency = "daily",
                    Interval = 1,
                    Until = new DateTimeOffset(2026, 3, 26, 8, 0, 0, TimeSpan.FromHours(1)),
                },
            }
        );
        using var invalidRecurrenceResponse = await client.PostAsync(
            "/api/calendar/events",
            invalidRecurrenceContent,
            TestContext.Current.CancellationToken
        );

        using var rangeResponse = await client.GetAsync(
            CalendarTestSupport.EventsUrl(RangeStart, RangeEnd),
            TestContext.Current.CancellationToken
        );

        await Verify(
            new
            {
                InvalidAllDayResponse = await HttpResponseSnapshot.CreateAsync(invalidAllDayResponse),
                InvalidRecurrenceResponse = await HttpResponseSnapshot.CreateAsync(invalidRecurrenceResponse),
                RangeResponse = await CalendarTestSupport.RangeSnapshotAsync(rangeResponse),
            }
        );
    }
}
