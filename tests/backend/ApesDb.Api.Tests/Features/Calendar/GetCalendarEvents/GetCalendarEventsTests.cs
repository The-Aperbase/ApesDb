using ApesDb.Api.Features.Calendar;
using ApesDb.Api.Tests.Infrastructure.Authentication;
using ApesDb.Api.Tests.Infrastructure.Factories;
using ApesDb.Api.Tests.Infrastructure.Http;

namespace ApesDb.Api.Tests.Features.Calendar.GetCalendarEvents;

public sealed class GetCalendarEventsTests
{
    private static readonly DateTimeOffset RangeStart = new(2026, 1, 12, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset RangeEnd = new(2026, 1, 19, 0, 0, 0, TimeSpan.Zero);

    private readonly SharedGetApiFactory _factory;

    public GetCalendarEventsTests(SharedGetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AnonymousUserCannotGetCalendarEvents()
    {
        using var client = ApiTestClient.CreateAnonymous(_factory);
        using var response = await client.GetAsync(
            CalendarTestSupport.EventsUrl(RangeStart, RangeEnd),
            TestContext.Current.CancellationToken
        );

        await Verify(await HttpResponseSnapshot.CreateAsync<object>(response));
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("member")]
    [InlineData("invitee")]
    [InlineData("outsider")]
    public async Task UserSeesOwnAndConnectedCalendarEvents(string identityKey)
    {
        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Find(identityKey)!);
        using var response = await client.GetAsync(
            CalendarTestSupport.EventsUrl(RangeStart, RangeEnd),
            TestContext.Current.CancellationToken
        );

        await Verify(await HttpResponseSnapshot.CreateAsync<CalendarRangeResponse>(response))
            .UseParameters(identityKey);
    }

    [Fact]
    public async Task InvalidRangeIsRejected()
    {
        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var response = await client.GetAsync(
            CalendarTestSupport.EventsUrl(RangeStart, RangeStart.AddDays(63)),
            TestContext.Current.CancellationToken
        );

        await Verify(await HttpResponseSnapshot.CreateAsync(response));
    }
}
