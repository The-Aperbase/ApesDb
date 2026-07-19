using ApesDb.Domain;
using ApesDb.Domain.Entities.Teams;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.GamesLists.DeleteGamesList;

public sealed class DeleteGamesListEndpoint : Endpoint<DeleteGamesListRequest>
{
    private readonly ApplicationDbContext _dbContext;

    public DeleteGamesListEndpoint(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Delete(ApiRoutes.GamesLists.ById);
        Summary(summary => summary.Summary = "Deletes a games list and its entries.");
    }

    public override async Task HandleAsync(DeleteGamesListRequest request, CancellationToken ct)
    {
        var userId = User.GetApesDbUserId();
        var deleted = await _dbContext
            .GamesLists.Where(list =>
                list.Id == request.ListId
                && list.TeamId == request.TeamId
                && _dbContext.TeamMemberships.Any(membership =>
                    membership.TeamId == list.TeamId
                    && membership.UserId == userId
                    && membership.Status == TeamMembershipStatus.Accepted
                )
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
