namespace ApesDb.Api.Features.Boards.Sharing.RespondToBoardInvitation;

public sealed class RespondToBoardInvitationRequest
{
    public Guid BoardId { get; init; }

    public Guid InvitationId { get; init; }

    public bool Accept { get; init; }
}
