using ApesDb.Api.Tests.Infrastructure.Authentication;
using ApesDb.Domain.Entities.Boards;
using ApesDb.Domain.Entities.Users;

namespace ApesDb.Api.Tests.TestData;

public static class BoardCollaborationTestData
{
    public static readonly Guid PendingInvitationId = Guid.Parse("01910000-0000-7000-8000-00000000a001");

    public static object[] Create(
        IReadOnlyDictionary<Guid, Board> boardsById,
        IReadOnlyDictionary<Guid, User> usersById
    )
    {
        var memberId = TestUsers.Member.SeededUserId!.Value;
        var inviteeId = TestUsers.Invitee.SeededUserId!.Value;
        var backlog = boardsById[BoardTestData.BacklogId];

        return
        [
            new BoardCollaborator
            {
                BoardId = backlog.Id,
                Board = backlog,
                UserId = memberId,
                User = usersById[memberId],
                JoinedAt = new DateTime(2026, 1, 11, 12, 0, 0, DateTimeKind.Utc),
            },
            new BoardInvitation
            {
                Id = PendingInvitationId,
                BoardId = backlog.Id,
                Board = backlog,
                InviteeUserId = inviteeId,
                InviteeUser = usersById[inviteeId],
                InviteeEmail = TestUsers.Invitee.Email,
                StatusId = BoardInvitationStatus.Pending,
                CreatedAt = new DateTime(2026, 1, 11, 13, 0, 0, DateTimeKind.Utc),
            },
        ];
    }
}
