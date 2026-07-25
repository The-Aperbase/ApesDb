using ApesDb.Api.Features.Notifications.GetNotifications;
using ApesDb.Api.Tests.Infrastructure.Authentication;
using ApesDb.Api.Tests.Infrastructure.Factories;
using ApesDb.Api.Tests.Infrastructure.Http;

namespace ApesDb.Api.Tests.Features.Notifications.ReadNotifications;

public sealed class ReadNotificationsTests : IClassFixture<MutableEndpointApiFactory>, IAsyncLifetime
{
    private readonly MutableEndpointApiFactory _factory;

    public ReadNotificationsTests(MutableEndpointApiFactory factory)
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
    public async Task ExistingUserCanMarkActiveNotificationsAsRead()
    {
        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Invitee);
        var result = await ReadAndGetNotificationsAsync(client);

        await Verify(result);
    }

    [Fact]
    public async Task OnlyCurrentUsersNotificationsAreMarkedAsRead()
    {
        using var inviteeClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Invitee);
        var currentUserResult = await ReadAndGetNotificationsAsync(inviteeClient);

        using var outsiderClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Outsider);
        using var outsiderResponse = await outsiderClient.GetAsync(
            "/api/notifications",
            TestContext.Current.CancellationToken
        );
        var outsiderResult = await HttpResponseSnapshot.CreateAsync<NotificationsResponse>(outsiderResponse);

        await Verify(new { CurrentUser = currentUserResult, OtherUserNotificationsResponse = outsiderResult });
    }

    [Fact]
    public async Task ResolvedNotificationsRemainExcludedWhenReadingNotifications()
    {
        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Invitee);
        var result = await ReadAndGetNotificationsAsync(client);

        await Verify(result);
    }

    [Fact]
    public async Task ReadingNotificationsIsIdempotent()
    {
        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Invitee);
        using var firstResponse = await client.PostAsync(
            "/api/notifications/read",
            TestContext.Current.CancellationToken
        );
        using var secondResponse = await client.PostAsync(
            "/api/notifications/read",
            TestContext.Current.CancellationToken
        );

        var readResponses = await HttpResponseSnapshot.CreateAsync(firstResponse, secondResponse);
        using var notificationsResponse = await client.GetAsync(
            "/api/notifications",
            TestContext.Current.CancellationToken
        );
        var notifications = await HttpResponseSnapshot.CreateAsync<NotificationsResponse>(notificationsResponse);

        await Verify(new { ReadResponses = readResponses, NotificationsResponse = notifications });
    }

    [Fact]
    public async Task AnonymousUserCannotReadNotifications()
    {
        using var anonymousClient = ApiTestClient.CreateAnonymous(_factory);
        using var readResponse = await anonymousClient.PostAsync(
            "/api/notifications/read",
            TestContext.Current.CancellationToken
        );
        var readResult = await HttpResponseSnapshot.CreateAsync(readResponse);

        using var inviteeClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Invitee);
        using var notificationsResponse = await inviteeClient.GetAsync(
            "/api/notifications",
            TestContext.Current.CancellationToken
        );
        var notifications = await HttpResponseSnapshot.CreateAsync<NotificationsResponse>(notificationsResponse);

        await Verify(new { ReadResponse = readResult.Response, NotificationsResponse = notifications });
    }

    private static async Task<object> ReadAndGetNotificationsAsync(ApiTestClient client)
    {
        using var readResponse = await client.PostAsync(
            "/api/notifications/read",
            TestContext.Current.CancellationToken
        );
        var readResult = await HttpResponseSnapshot.CreateAsync(readResponse);
        using var notificationsResponse = await client.GetAsync(
            "/api/notifications",
            TestContext.Current.CancellationToken
        );
        var notifications = await HttpResponseSnapshot.CreateAsync<NotificationsResponse>(notificationsResponse);

        return new { ReadResponse = readResult.Response, NotificationsResponse = notifications };
    }
}
