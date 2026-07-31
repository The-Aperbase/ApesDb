using System.Net.Http.Json;
using ApesDb.Api.Features.Auth.Me;
using ApesDb.Api.Features.Boards.Sharing;
using ApesDb.Api.Features.Notifications.GetNotifications;
using ApesDb.Api.Tests.Infrastructure.Authentication;
using ApesDb.Api.Tests.Infrastructure.Factories;
using ApesDb.Api.Tests.Infrastructure.Http;
using ApesDb.Api.Tests.TestData;

namespace ApesDb.Api.Tests.Features.Boards.Sharing;

public sealed class BoardSharingTests : IClassFixture<MutableEndpointApiFactory>, IAsyncLifetime
{
    private readonly MutableEndpointApiFactory _factory;

    public BoardSharingTests(MutableEndpointApiFactory factory)
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
    public async Task OwnerCanInviteExistingUserAndDuplicateInvitationsAreIgnored()
    {
        using var ownerClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var firstResponse = await ownerClient.PostAsJsonAsync(
            $"{BoardTestSupport.BoardUrl(BoardTestData.BacklogId)}/invitations",
            new { Email = $"  {TestUsers.Outsider.Email.ToUpperInvariant()}  " },
            TestContext.Current.CancellationToken
        );
        using var duplicateResponse = await ownerClient.PostAsJsonAsync(
            $"{BoardTestSupport.BoardUrl(BoardTestData.BacklogId)}/invitations",
            new { Email = TestUsers.Outsider.Email },
            TestContext.Current.CancellationToken
        );
        using var selfResponse = await ownerClient.PostAsJsonAsync(
            $"{BoardTestSupport.BoardUrl(BoardTestData.BacklogId)}/invitations",
            new { Email = TestUsers.Owner.Email },
            TestContext.Current.CancellationToken
        );
        using var collaboratorResponse = await ownerClient.PostAsJsonAsync(
            $"{BoardTestSupport.BoardUrl(BoardTestData.BacklogId)}/invitations",
            new { Email = TestUsers.Member.Email },
            TestContext.Current.CancellationToken
        );
        using var sharingResponse = await ownerClient.GetAsync(
            $"{BoardTestSupport.BoardUrl(BoardTestData.BacklogId)}/sharing",
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
                FirstResponse = await HttpResponseSnapshot.CreateAsync<BoardInvitationResponse>(firstResponse),
                DuplicateResponse = HttpResponseSnapshot.CreateWithoutContent(duplicateResponse),
                SelfResponse = HttpResponseSnapshot.CreateWithoutContent(selfResponse),
                CollaboratorResponse = HttpResponseSnapshot.CreateWithoutContent(collaboratorResponse),
                SharingResponse = await HttpResponseSnapshot.CreateAsync<BoardSharingResponse>(sharingResponse),
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
        using var inviteResponse = await ownerClient.PostAsJsonAsync(
            $"{BoardTestSupport.BoardUrl(BoardTestData.CompletedId)}/invitations",
            new { Email = TestUsers.SignupCandidate.Email },
            TestContext.Current.CancellationToken
        );
        using var signupClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.SignupCandidate);
        using var signupResponse = await signupClient.GetAsync(
            "/api/auth/me",
            TestContext.Current.CancellationToken
        );
        using var notificationsResponse = await signupClient.GetAsync(
            "/api/notifications",
            TestContext.Current.CancellationToken
        );

        await Verify(
            new
            {
                InviteResponse = await HttpResponseSnapshot.CreateAsync<BoardInvitationResponse>(inviteResponse),
                SignupResponse = await HttpResponseSnapshot.CreateAsync<AuthUserResponse>(signupResponse),
                NotificationsResponse = await HttpResponseSnapshot.CreateAsync<NotificationsResponse>(
                    notificationsResponse
                ),
            }
        );
    }

    [Fact]
    public async Task InviteeCanAcceptInvitationAndAccessBoard()
    {
        using var inviteeClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Invitee);
        using var respondResponse = await inviteeClient.PostAsJsonAsync(
            $"{BoardTestSupport.BoardUrl(BoardTestData.BacklogId)}/invitations/{BoardCollaborationTestData.PendingInvitationId}/respond",
            new { Accept = true },
            TestContext.Current.CancellationToken
        );
        using var repeatedResponse = await inviteeClient.PostAsJsonAsync(
            $"{BoardTestSupport.BoardUrl(BoardTestData.BacklogId)}/invitations/{BoardCollaborationTestData.PendingInvitationId}/respond",
            new { Accept = true },
            TestContext.Current.CancellationToken
        );
        using var declineAfterAcceptResponse = await inviteeClient.PostAsJsonAsync(
            $"{BoardTestSupport.BoardUrl(BoardTestData.BacklogId)}/invitations/{BoardCollaborationTestData.PendingInvitationId}/respond",
            new { Accept = false },
            TestContext.Current.CancellationToken
        );
        using var listResponse = await inviteeClient.GetAsync("/api/boards", TestContext.Current.CancellationToken);
        using var boardResponse = await inviteeClient.GetAsync(
            BoardTestSupport.BoardUrl(BoardTestData.BacklogId),
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
                RepeatedResponse = HttpResponseSnapshot.CreateWithoutContent(repeatedResponse),
                DeclineAfterAcceptResponse = HttpResponseSnapshot.CreateWithoutContent(declineAfterAcceptResponse),
                ListResponse = await BoardTestSupport.ListSnapshotAsync(listResponse),
                BoardResponse = await BoardTestSupport.DetailsSnapshotAsync(boardResponse),
                NotificationsResponse = await HttpResponseSnapshot.CreateAsync<NotificationsResponse>(
                    notificationsResponse
                ),
            }
        );
    }

    [Fact]
    public async Task AcceptedCollaboratorCanAddMoveAndRemoveGames()
    {
        using var memberClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Member);
        using var addResponse = await memberClient.PostAsJsonAsync(
            BoardTestSupport.EntriesUrl(BoardTestData.BacklogId),
            new { GameId = BoardEntryTestData.AddableGameId },
            TestContext.Current.CancellationToken
        );
        using var moveResponse = await memberClient.PutAsJsonAsync(
            BoardTestSupport.EntryUrl(BoardTestData.BacklogId, BoardEntryTestData.AddableGameId),
            new { State = "completed", Position = 1 },
            TestContext.Current.CancellationToken
        );
        using var afterMoveResponse = await memberClient.GetAsync(
            BoardTestSupport.BoardUrl(BoardTestData.BacklogId),
            TestContext.Current.CancellationToken
        );
        using var removeResponse = await memberClient.DeleteAsync(
            BoardTestSupport.EntryUrl(BoardTestData.BacklogId, BoardEntryTestData.AddableGameId),
            TestContext.Current.CancellationToken
        );
        using var finalResponse = await memberClient.GetAsync(
            BoardTestSupport.BoardUrl(BoardTestData.BacklogId),
            TestContext.Current.CancellationToken
        );

        await Verify(
            new
            {
                AddResponse = await HttpResponseSnapshot.CreateAsync(addResponse),
                MoveResponse = HttpResponseSnapshot.CreateWithoutContent(moveResponse),
                AfterMoveResponse = await BoardTestSupport.DetailsSnapshotAsync(afterMoveResponse),
                RemoveResponse = HttpResponseSnapshot.CreateWithoutContent(removeResponse),
                FinalResponse = await BoardTestSupport.DetailsSnapshotAsync(finalResponse),
            }
        );
    }

    [Fact]
    public async Task InviteeCanDeclineInvitationWithoutGainingAccess()
    {
        using var inviteeClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Invitee);
        using var respondResponse = await inviteeClient.PostAsJsonAsync(
            $"{BoardTestSupport.BoardUrl(BoardTestData.BacklogId)}/invitations/{BoardCollaborationTestData.PendingInvitationId}/respond",
            new { Accept = false },
            TestContext.Current.CancellationToken
        );
        using var boardResponse = await inviteeClient.GetAsync(
            BoardTestSupport.BoardUrl(BoardTestData.BacklogId),
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
                BoardResponse = await HttpResponseSnapshot.CreateAsync(boardResponse),
                NotificationsResponse = await HttpResponseSnapshot.CreateAsync<NotificationsResponse>(
                    notificationsResponse
                ),
            }
        );
    }

    [Fact]
    public async Task CollaboratorCannotAdministerBoard()
    {
        using var memberClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Member);
        using var updateForm = BoardTestSupport.CreateForm("Not allowed");
        using var updateResponse = await memberClient.PutMultipartAsync(
            BoardTestSupport.BoardUrl(BoardTestData.BacklogId),
            updateForm,
            TestContext.Current.CancellationToken
        );
        using var deleteResponse = await memberClient.DeleteAsync(
            BoardTestSupport.BoardUrl(BoardTestData.BacklogId),
            TestContext.Current.CancellationToken
        );
        using var inviteResponse = await memberClient.PostAsJsonAsync(
            $"{BoardTestSupport.BoardUrl(BoardTestData.BacklogId)}/invitations",
            new { Email = TestUsers.Outsider.Email },
            TestContext.Current.CancellationToken
        );
        using var cancelResponse = await memberClient.DeleteAsync(
            $"{BoardTestSupport.BoardUrl(BoardTestData.BacklogId)}/invitations/{BoardCollaborationTestData.PendingInvitationId}",
            TestContext.Current.CancellationToken
        );
        using var removeResponse = await memberClient.DeleteAsync(
            $"{BoardTestSupport.BoardUrl(BoardTestData.BacklogId)}/collaborators/{TestUsers.Invitee.SeededUserId}",
            TestContext.Current.CancellationToken
        );
        using var ownerClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var boardResponse = await ownerClient.GetAsync(
            BoardTestSupport.BoardUrl(BoardTestData.BacklogId),
            TestContext.Current.CancellationToken
        );
        using var sharingResponse = await ownerClient.GetAsync(
            $"{BoardTestSupport.BoardUrl(BoardTestData.BacklogId)}/sharing",
            TestContext.Current.CancellationToken
        );

        await Verify(
            new
            {
                UpdateResponse = await HttpResponseSnapshot.CreateAsync(updateResponse),
                DeleteResponse = await HttpResponseSnapshot.CreateAsync(deleteResponse),
                InviteResponse = await HttpResponseSnapshot.CreateAsync(inviteResponse),
                CancelResponse = await HttpResponseSnapshot.CreateAsync(cancelResponse),
                RemoveResponse = await HttpResponseSnapshot.CreateAsync(removeResponse),
                BoardResponse = await BoardTestSupport.DetailsSnapshotAsync(boardResponse),
                SharingResponse = await HttpResponseSnapshot.CreateAsync<BoardSharingResponse>(sharingResponse),
            }
        );
    }

    [Fact]
    public async Task OwnerCanRevokeAndCollaboratorCanLeaveBoard()
    {
        using var ownerClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var revokeResponse = await ownerClient.DeleteAsync(
            $"{BoardTestSupport.BoardUrl(BoardTestData.BacklogId)}/collaborators/{TestUsers.Member.SeededUserId}",
            TestContext.Current.CancellationToken
        );
        using var memberClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Member);
        using var revokedBoardResponse = await memberClient.GetAsync(
            BoardTestSupport.BoardUrl(BoardTestData.BacklogId),
            TestContext.Current.CancellationToken
        );
        using var reinviteResponse = await ownerClient.PostAsJsonAsync(
            $"{BoardTestSupport.BoardUrl(BoardTestData.BacklogId)}/invitations",
            new { Email = TestUsers.Member.Email },
            TestContext.Current.CancellationToken
        );

        await _factory.ResetAsync(TestContext.Current.CancellationToken);
        using var leaveResponse = await memberClient.DeleteAsync(
            $"{BoardTestSupport.BoardUrl(BoardTestData.BacklogId)}/collaborators/{TestUsers.Member.SeededUserId}",
            TestContext.Current.CancellationToken
        );
        using var leftBoardResponse = await memberClient.GetAsync(
            BoardTestSupport.BoardUrl(BoardTestData.BacklogId),
            TestContext.Current.CancellationToken
        );

        await Verify(
            new
            {
                RevokeResponse = HttpResponseSnapshot.CreateWithoutContent(revokeResponse),
                RevokedBoardResponse = await HttpResponseSnapshot.CreateAsync(revokedBoardResponse),
                ReinviteResponse = await HttpResponseSnapshot.CreateAsync<BoardInvitationResponse>(reinviteResponse),
                LeaveResponse = HttpResponseSnapshot.CreateWithoutContent(leaveResponse),
                LeftBoardResponse = await HttpResponseSnapshot.CreateAsync(leftBoardResponse),
            }
        );
    }

    [Fact]
    public async Task CancellingInvitationResolvesNotification()
    {
        using var ownerClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var cancelResponse = await ownerClient.DeleteAsync(
            $"{BoardTestSupport.BoardUrl(BoardTestData.BacklogId)}/invitations/{BoardCollaborationTestData.PendingInvitationId}",
            TestContext.Current.CancellationToken
        );
        using var inviteeClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Invitee);
        using var notificationsResponse = await inviteeClient.GetAsync(
            "/api/notifications",
            TestContext.Current.CancellationToken
        );

        await Verify(
            new
            {
                CancelResponse = HttpResponseSnapshot.CreateWithoutContent(cancelResponse),
                NotificationsResponse = await HttpResponseSnapshot.CreateAsync<NotificationsResponse>(
                    notificationsResponse
                ),
            }
        );
    }

    [Fact]
    public async Task DeletingBoardResolvesInvitationNotification()
    {
        using var ownerClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var deleteResponse = await ownerClient.DeleteAsync(
            BoardTestSupport.BoardUrl(BoardTestData.BacklogId),
            TestContext.Current.CancellationToken
        );
        using var inviteeClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Invitee);
        using var notificationsResponse = await inviteeClient.GetAsync(
            "/api/notifications",
            TestContext.Current.CancellationToken
        );

        await Verify(
            new
            {
                DeleteResponse = HttpResponseSnapshot.CreateWithoutContent(deleteResponse),
                NotificationsResponse = await HttpResponseSnapshot.CreateAsync<NotificationsResponse>(
                    notificationsResponse
                ),
            }
        );
    }

    [Fact]
    public async Task AnonymousUserCannotManageBoardSharing()
    {
        using var client = ApiTestClient.CreateAnonymous(_factory);
        using var response = await client.GetAsync(
            $"{BoardTestSupport.BoardUrl(BoardTestData.BacklogId)}/sharing",
            TestContext.Current.CancellationToken
        );

        await Verify(await HttpResponseSnapshot.CreateAsync(response));
    }
}
