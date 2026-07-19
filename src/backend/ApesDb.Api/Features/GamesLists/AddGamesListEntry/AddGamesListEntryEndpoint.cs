using ApesDb.Common;
using ApesDb.Domain;
using ApesDb.Domain.Entities.Teams;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.GamesLists.AddGamesListEntry;

public sealed class AddGamesListEntryEndpoint : Endpoint<AddGamesListEntryRequest>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddGamesListEntryEndpoint(ApplicationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public override void Configure()
    {
        Post(ApiRoutes.GamesLists.Entries);
        Summary(summary => summary.Summary = "Adds a game to a games list.");
    }

    public override async Task HandleAsync(AddGamesListEntryRequest request, CancellationToken ct)
    {
        var userId = User.GetApesDbUserId();
        var canAccess = await _dbContext.GamesLists.AnyAsync(
            list =>
                list.Id == request.ListId
                && list.TeamId == request.TeamId
                && _dbContext.TeamMemberships.Any(membership =>
                    membership.TeamId == list.TeamId
                    && membership.UserId == userId
                    && membership.Status == TeamMembershipStatus.Accepted
                ),
            ct
        );
        if (!canAccess)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var gameExists = await _dbContext.Games.AnyAsync(game => game.Id == request.GameId, ct);
        if (!gameExists)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "public"."GamesListEntries" ("GamesListId", "GameId")
            VALUES ({request.ListId}, {request.GameId})
            ON CONFLICT ("GamesListId", "GameId") DO NOTHING
            """,
            ct
        );
        await _dbContext
            .GamesLists.Where(list => list.Id == request.ListId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(list => list.UpdatedAt, _dateTimeProvider.UtcNow),
                ct
            );

        await Send.NoContentAsync(ct);
    }
}
