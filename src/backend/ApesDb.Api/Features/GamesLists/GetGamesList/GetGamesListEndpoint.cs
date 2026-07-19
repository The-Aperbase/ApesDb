using ApesDb.Domain;
using ApesDb.Domain.Entities.Teams;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.GamesLists.GetGamesList;

public sealed class GetGamesListEndpoint : Endpoint<GetGamesListRequest, GamesListDetailsResponse>
{
    private readonly ApplicationDbContext _dbContext;

    public GetGamesListEndpoint(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get(ApiRoutes.GamesLists.ById);
        Summary(summary => summary.Summary = "Gets a games list and its games.");
    }

    public override async Task HandleAsync(GetGamesListRequest request, CancellationToken ct)
    {
        var userId = User.GetApesDbUserId();
        var list = await _dbContext
            .GamesLists.AsNoTracking()
            .Where(list =>
                list.Id == request.ListId
                && list.TeamId == request.TeamId
                && _dbContext.TeamMemberships.Any(membership =>
                    membership.TeamId == list.TeamId
                    && membership.UserId == userId
                    && membership.Status == TeamMembershipStatus.Accepted
                )
            )
            .Select(list => new
            {
                list.Id,
                list.Name,
                list.Picture,
                list.CreatedAt,
                list.UpdatedAt,
            })
            .SingleOrDefaultAsync(ct);

        if (list is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var games = await GamesListGameLoader.LoadAsync(_dbContext, request.ListId, ct);

        await Send.OkAsync(
            new GamesListDetailsResponse(
                list.Id,
                list.Name,
                list.CreatedAt,
                list.UpdatedAt,
                GamesListResponseFactory.CreatePicture(list.Picture),
                games
            ),
            ct
        );
    }
}
