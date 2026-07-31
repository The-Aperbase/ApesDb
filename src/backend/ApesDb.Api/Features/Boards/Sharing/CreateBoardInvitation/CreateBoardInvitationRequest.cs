namespace ApesDb.Api.Features.Boards.Sharing.CreateBoardInvitation;

public sealed class CreateBoardInvitationRequest
{
    public Guid BoardId { get; init; }

    public string Email { get; init; } = string.Empty;
}
