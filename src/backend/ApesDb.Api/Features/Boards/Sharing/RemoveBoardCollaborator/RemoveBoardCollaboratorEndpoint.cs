using ApesDb.Domain;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Boards.Sharing.RemoveBoardCollaborator;

public sealed class RemoveBoardCollaboratorEndpoint : Endpoint<RemoveBoardCollaboratorRequest>
{
    private readonly ApplicationDbContext _dbContext;

    public RemoveBoardCollaboratorEndpoint(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Delete(ApiRoutes.Boards.CollaboratorByUser);
        Summary(summary => summary.Summary = "Revokes collaboration or leaves a shared board.");
    }

    public override async Task HandleAsync(RemoveBoardCollaboratorRequest request, CancellationToken ct)
    {
        var userId = User.GetApesDbUserId();
        var mayRemove = request.CollaboratorUserId == userId;
        if (!mayRemove)
        {
            mayRemove = await _dbContext.Boards.AnyAsync(
                board => board.Id == request.BoardId && board.OwnerUserId == userId,
                ct
            );
        }

        if (!mayRemove)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var deleted = await _dbContext
            .BoardCollaborators.Where(collaborator =>
                collaborator.BoardId == request.BoardId && collaborator.UserId == request.CollaboratorUserId
            )
            .ExecuteDeleteAsync(ct);
        if (deleted == 0)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
