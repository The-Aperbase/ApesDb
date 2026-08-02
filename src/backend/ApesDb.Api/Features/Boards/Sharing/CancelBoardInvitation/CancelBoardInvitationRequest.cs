namespace ApesDb.Api.Features.Boards.Sharing.CancelBoardInvitation;

public sealed class CancelBoardInvitationRequest
{
    public Guid BoardId { get; init; }

    public Guid InvitationId { get; init; }
}
