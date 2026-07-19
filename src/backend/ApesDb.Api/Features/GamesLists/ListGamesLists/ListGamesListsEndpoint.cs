using ApesDb.Domain;
using ApesDb.Domain.Entities.Teams;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.GamesLists.ListGamesLists;

public sealed class ListGamesListsEndpoint : Endpoint<ListGamesListsRequest, GamesListSummaryResponse[]>
{
    private readonly ApplicationDbContext _dbContext;

    public ListGamesListsEndpoint(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get(ApiRoutes.GamesLists.List);
        Summary(summary =>
        {
            summary.Summary = "Lists the games lists of a team.";
            summary.Description =
                "When a game id is provided, each list reports whether it already contains that game.";
        });
    }

    public override async Task HandleAsync(ListGamesListsRequest request, CancellationToken ct)
    {
        var userId = User.GetApesDbUserId();

        var lists = await _dbContext
            .GamesLists.AsNoTracking()
            .Where(list =>
                list.TeamId == request.TeamId
                && _dbContext.TeamMemberships.Any(membership =>
                    membership.TeamId == list.TeamId
                    && membership.UserId == userId
                    && membership.Status == TeamMembershipStatus.Accepted
                )
            )
            .OrderBy(list => list.Name.ToLower())
            .ThenBy(list => list.Name)
            .ThenBy(list => list.Id)
            .Select(list => new
            {
                list.Id,
                list.Name,
                list.Picture,
                list.CreatedAt,
                list.UpdatedAt,
                GameCount = list.Entries.Count,
                ContainsGame =
                    request.GameId != null && list.Entries.Any(entry => entry.GameId == request.GameId),
            })
            .ToArrayAsync(ct);

        var response = lists
            .Select(list => new GamesListSummaryResponse(
                list.Id,
                list.Name,
                list.CreatedAt,
                list.UpdatedAt,
                GamesListResponseFactory.CreatePicture(list.Picture),
                list.GameCount,
                list.ContainsGame
            ))
            .ToArray();

        await Send.OkAsync(response, ct);
    }
}
