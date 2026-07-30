using System.Net.Http.Json;
using ApesDb.Api.Features.Auth.Me;
using ApesDb.Api.Features.Calendar;
using ApesDb.Api.Features.Calendar.Sharing;
using ApesDb.Api.Features.Calendar.Sharing.CreateCalendarInvitation;
using ApesDb.Api.Features.Calendar.Sharing.RespondToCalendarInvitation;
using ApesDb.Api.Features.Notifications.GetNotifications;
using ApesDb.Api.Tests.Infrastructure.Authentication;
using ApesDb.Api.Tests.Infrastructure.Factories;
using ApesDb.Api.Tests.Infrastructure.Http;
using ApesDb.Api.Tests.TestData;

namespace ApesDb.Api.Tests.Features.Calendar;

public sealed class CalendarSharingTests : IClassFixture<MutableEndpointApiFactory>, IAsyncLifetime
{
    private readonly MutableEndpointApiFactory _factory;

    public CalendarSharingTests(MutableEndpointApiFactory factory)
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
    public async Task InvitationCreatesActionableNotification()
    {
        using var ownerClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var content = JsonContent.Create(
            new CreateCalendarInvitationRequest { Email = $"  {TestUsers.Outsider.Email.ToUpperInvariant()}  " }
        );
        using var inviteResponse = await ownerClient.PostAsync(
            "/api/calendar/invites",
            content,
            TestContext.Current.CancellationToken
        );
        var invitation =
            await inviteResponse.Content.ReadFromJsonAsync<CalendarInvitationResponse>(
                TestContext.Current.CancellationToken
            ) ?? throw new InvalidOperationException("The invitation response did not contain a resource.");

        using var sharingResponse = await ownerClient.GetAsync(
            "/api/calendar/sharing",
            TestContext.Current.CancellationToken
        );
        using var outsiderClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Outsider);
        using var invitationResponse = await outsiderClient.GetAsync(
            $"/api/calendar/invites/{invitation.Id}",
            TestContext.Current.CancellationToken
        );
        using var notificationsResponse = await outsiderClient.GetAsync(
            "/api/notifications",
            TestContext.Current.CancellationToken
        );

        await Verify(
            new
            {
                InviteResponse = await HttpResponseSnapshot.CreateAsync<CalendarInvitationResponse>(inviteResponse),
                InvitationResponse = await HttpResponseSnapshot.CreateAsync<CalendarInvitationResponse>(
                    invitationResponse
                ),
                SharingResponse = await HttpResponseSnapshot.CreateAsync<CalendarSharingResponse>(sharingResponse),
                NotificationsResponse = await HttpResponseSnapshot.CreateAsync<NotificationsResponse>(
                    notificationsResponse
                ),
            }
        );
    }

    [Fact]
    public async Task InvitationToUnregisteredUserIsLinkedWhenTheySignUp()
    {
        using var ownerClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var inviteContent = JsonContent.Create(
            new CreateCalendarInvitationRequest { Email = TestUsers.SignupCandidate.Email }
        );
        using var inviteResponse = await ownerClient.PostAsync(
            "/api/calendar/invites",
            inviteContent,
            TestContext.Current.CancellationToken
        );
        var invitation =
            await inviteResponse.Content.ReadFromJsonAsync<CalendarInvitationResponse>(
                TestContext.Current.CancellationToken
            ) ?? throw new InvalidOperationException("The invitation response did not contain a resource.");
        using var sharingBeforeSignupResponse = await ownerClient.GetAsync(
            "/api/calendar/sharing",
            TestContext.Current.CancellationToken
        );

        using var signupClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.SignupCandidate);
        using var signupResponse = await signupClient.GetAsync("/api/auth/me", TestContext.Current.CancellationToken);
        using var invitationResponse = await signupClient.GetAsync(
            $"/api/calendar/invites/{invitation.Id}",
            TestContext.Current.CancellationToken
        );
        using var notificationsResponse = await signupClient.GetAsync(
            "/api/notifications",
            TestContext.Current.CancellationToken
        );

        await Verify(
            new
            {
                InviteResponse = await HttpResponseSnapshot.CreateAsync<CalendarInvitationResponse>(inviteResponse),
                SharingBeforeSignupResponse = await HttpResponseSnapshot.CreateAsync<CalendarSharingResponse>(
                    sharingBeforeSignupResponse
                ),
                SignupResponse = await HttpResponseSnapshot.CreateAsync<AuthUserResponse>(signupResponse),
                InvitationResponse = await HttpResponseSnapshot.CreateAsync<CalendarInvitationResponse>(
                    invitationResponse
                ),
                NotificationsResponse = await HttpResponseSnapshot.CreateAsync<NotificationsResponse>(
                    notificationsResponse
                ),
            }
        );
    }

    [Fact]
    public async Task DuplicateInvitationIsIdempotent()
    {
        using var ownerClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var firstContent = JsonContent.Create(
            new CreateCalendarInvitationRequest { Email = TestUsers.Outsider.Email }
        );
        using var firstResponse = await ownerClient.PostAsync(
            "/api/calendar/invites",
            firstContent,
            TestContext.Current.CancellationToken
        );
        using var secondContent = JsonContent.Create(
            new CreateCalendarInvitationRequest { Email = TestUsers.Outsider.Email }
        );
        using var secondResponse = await ownerClient.PostAsync(
            "/api/calendar/invites",
            secondContent,
            TestContext.Current.CancellationToken
        );
        using var sharingResponse = await ownerClient.GetAsync(
            "/api/calendar/sharing",
            TestContext.Current.CancellationToken
        );
        using var outsiderClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Outsider);
        using var notificationsResponse = await outsiderClient.GetAsync(
            "/api/notifications",
            TestContext.Current.CancellationToken
        );

        await Verify(
            new
            {
                FirstResponse = await HttpResponseSnapshot.CreateAsync<CalendarInvitationResponse>(firstResponse),
                SecondResponse = HttpResponseSnapshot.CreateWithoutContent(secondResponse),
                SharingResponse = await HttpResponseSnapshot.CreateAsync<CalendarSharingResponse>(sharingResponse),
                NotificationsResponse = await HttpResponseSnapshot.CreateAsync<NotificationsResponse>(
                    notificationsResponse
                ),
            }
        );
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("member")]
    public async Task SelfAndConnectedInvitationsAreIgnored(string inviteeKey)
    {
        var invitee = TestUsers.Find(inviteeKey)!;
        using var ownerClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var content = JsonContent.Create(new CreateCalendarInvitationRequest { Email = invitee.Email });
        using var inviteResponse = await ownerClient.PostAsync(
            "/api/calendar/invites",
            content,
            TestContext.Current.CancellationToken
        );
        using var sharingResponse = await ownerClient.GetAsync(
            "/api/calendar/sharing",
            TestContext.Current.CancellationToken
        );
        using var inviteeClient = ApiTestClient.CreateAuthenticated(_factory, invitee);
        using var notificationsResponse = await inviteeClient.GetAsync(
            "/api/notifications",
            TestContext.Current.CancellationToken
        );

        await Verify(
                new
                {
                    InviteResponse = HttpResponseSnapshot.CreateWithoutContent(inviteResponse),
                    SharingResponse = await HttpResponseSnapshot.CreateAsync<CalendarSharingResponse>(sharingResponse),
                    NotificationsResponse = await HttpResponseSnapshot.CreateAsync<NotificationsResponse>(
                        notificationsResponse
                    ),
                }
            )
            .UseParameters(inviteeKey);
    }

    [Fact]
    public async Task AcceptingInvitationCreatesMutualCalendarAccess()
    {
        using var inviteeClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Invitee);
        using var content = JsonContent.Create(new RespondToCalendarInvitationRequest { Accept = true });
        using var respondResponse = await inviteeClient.PostAsync(
            $"/api/calendar/invites/{CalendarTestData.PendingInvitationId}/respond",
            content,
            TestContext.Current.CancellationToken
        );
        using var sharingResponse = await inviteeClient.GetAsync(
            "/api/calendar/sharing",
            TestContext.Current.CancellationToken
        );
        using var rangeResponse = await inviteeClient.GetAsync(
            CalendarTestSupport.EventsUrl(
                new DateTimeOffset(2026, 1, 12, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 1, 19, 0, 0, 0, TimeSpan.Zero)
            ),
            TestContext.Current.CancellationToken
        );
        using var notificationsResponse = await inviteeClient.GetAsync(
            "/api/notifications",
            TestContext.Current.CancellationToken
        );

        await Verify(
            new
            {
                RespondResponse = HttpResponseSnapshot.CreateWithoutContent(respondResponse),
                SharingResponse = await HttpResponseSnapshot.CreateAsync<CalendarSharingResponse>(sharingResponse),
                RangeResponse = await CalendarTestSupport.RangeSnapshotAsync(rangeResponse),
                NotificationsResponse = await HttpResponseSnapshot.CreateAsync<NotificationsResponse>(
                    notificationsResponse
                ),
            }
        );
    }

    [Fact]
    public async Task EitherUserCanDisconnectCalendarAccess()
    {
        using var memberClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Member);
        using var disconnectResponse = await memberClient.DeleteAsync(
            $"/api/calendar/connections/{CalendarTestData.OwnerMemberConnectionId}",
            TestContext.Current.CancellationToken
        );
        using var rangeResponse = await memberClient.GetAsync(
            CalendarTestSupport.EventsUrl(
                new DateTimeOffset(2026, 1, 12, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 1, 19, 0, 0, 0, TimeSpan.Zero)
            ),
            TestContext.Current.CancellationToken
        );

        await Verify(
            new
            {
                DisconnectResponse = HttpResponseSnapshot.CreateWithoutContent(disconnectResponse),
                RangeResponse = await CalendarTestSupport.RangeSnapshotAsync(rangeResponse),
            }
        );
    }
}
