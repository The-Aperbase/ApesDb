namespace ApesDb.Api.Features.Boards.Sharing;

public sealed record BoardCollaboratorResponse(BoardUserResponse User, DateTime JoinedAt);

public sealed record OutgoingBoardInvitationResponse(Guid Id, string Email, DateTime CreatedAt);

public sealed record BoardSharingResponse(
    BoardCollaboratorResponse[] Collaborators,
    OutgoingBoardInvitationResponse[] OutgoingInvitations
);

public sealed record BoardInvitationBoardResponse(Guid Id, string Name);

public sealed record BoardInvitationResponse(
    Guid Id,
    BoardInvitationBoardResponse Board,
    BoardUserResponse InvitedBy,
    DateTime CreatedAt
);
