namespace ApesDb.Api.Features.Boards.Sharing.RemoveBoardCollaborator;

public sealed class RemoveBoardCollaboratorRequest
{
    public Guid BoardId { get; init; }

    public Guid CollaboratorUserId { get; init; }
}
