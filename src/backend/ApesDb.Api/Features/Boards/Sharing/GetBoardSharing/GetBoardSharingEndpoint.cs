using ApesDb.Domain;
using ApesDb.Domain.Entities.Boards;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Boards.Sharing.GetBoardSharing;

public sealed class GetBoardSharingEndpoint : Endpoint<GetBoardSharingRequest, BoardSharingResponse>
{
    private readonly ApplicationDbContext _dbContext;

    public GetBoardSharingEndpoint(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Boards.Sharing);
        Summary(summary => summary.Summary = "Gets collaborators and pending invitations for an owned board.");
    }

    public override async Task HandleAsync(GetBoardSharingRequest request, CancellationToken ct)
    {
        var ownerUserId = User.GetApesDbUserId();
        var owned = await _dbContext.Boards.AnyAsync(
            board => board.Id == request.BoardId && board.OwnerUserId == ownerUserId,
            ct
        );
        if (!owned)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var collaborators = await _dbContext
            .BoardCollaborators.AsNoTracking()
            .Where(collaborator => collaborator.BoardId == request.BoardId)
            .OrderBy(collaborator => collaborator.User.Name.ToLower())
            .ThenBy(collaborator => collaborator.User.Name)
            .ThenBy(collaborator => collaborator.UserId)
            .Select(collaborator => new BoardCollaboratorResponse(
                new BoardUserResponse(collaborator.UserId, collaborator.User.Name, collaborator.User.PictureUrl),
                collaborator.JoinedAt
            ))
            .ToArrayAsync(ct);
        var invitations = await _dbContext
            .BoardInvitations.AsNoTracking()
            .Where(invitation =>
                invitation.BoardId == request.BoardId && invitation.StatusId == BoardInvitationStatus.Pending
            )
            .OrderByDescending(invitation => invitation.CreatedAt)
            .ThenByDescending(invitation => invitation.Id)
            .Select(invitation => new OutgoingBoardInvitationResponse(
                invitation.Id,
                invitation.InviteeEmail,
                invitation.CreatedAt
            ))
            .ToArrayAsync(ct);

        await Send.OkAsync(new BoardSharingResponse(collaborators, invitations), ct);
    }
}
