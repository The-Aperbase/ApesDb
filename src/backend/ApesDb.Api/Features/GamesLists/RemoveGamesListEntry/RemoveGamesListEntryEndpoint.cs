using ApesDb.Common;
using ApesDb.Domain;
using ApesDb.Domain.Entities.Teams;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.GamesLists.RemoveGamesListEntry;

public sealed class RemoveGamesListEntryEndpoint : Endpoint<RemoveGamesListEntryRequest>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RemoveGamesListEntryEndpoint(ApplicationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public override void Configure()
    {
        Delete(ApiRoutes.GamesLists.EntryByGame);
        Summary(summary => summary.Summary = "Removes a game from a games list.");
    }

    public override async Task HandleAsync(RemoveGamesListEntryRequest request, CancellationToken ct)
    {
        var userId = User.GetApesDbUserId();
        var deleted = await _dbContext
            .GamesListEntries.Where(entry =>
                entry.GamesListId == request.ListId
                && entry.GameId == request.GameId
                && _dbContext.GamesLists.Any(list =>
                    list.Id == entry.GamesListId
                    && list.TeamId == request.TeamId
                    && _dbContext.TeamMemberships.Any(membership =>
                        membership.TeamId == list.TeamId
                        && membership.UserId == userId
                        && membership.Status == TeamMembershipStatus.Accepted
                    )
                )
            )
            .ExecuteDeleteAsync(ct);

        if (deleted == 0)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await _dbContext
            .GamesLists.Where(list => list.Id == request.ListId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(list => list.UpdatedAt, _dateTimeProvider.UtcNow),
                ct
            );

        await Send.NoContentAsync(ct);
    }
}
