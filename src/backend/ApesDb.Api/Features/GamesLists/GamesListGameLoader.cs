using ApesDb.Domain;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.GamesLists;

internal static class GamesListGameLoader
{
    public static async Task<GamesListGameResponse[]> LoadAsync(
        ApplicationDbContext dbContext,
        Guid listId,
        CancellationToken ct
    )
    {
        var entries = await dbContext
            .GamesListEntries.AsNoTracking()
            .Where(entry => entry.GamesListId == listId)
            .OrderBy(entry => entry.AddedAt)
            .ThenBy(entry => entry.GameId)
            .Select(entry => new
            {
                entry.GameId,
                entry.Game.Name,
                entry.Game.CoverSmallUrl,
                entry.Game.CoverLargeUrl,
                GameType = dbContext
                    .GameTypes.Where(gameType => gameType.Id == entry.Game.GameTypeId)
                    .Select(gameType => gameType.Name)
                    .FirstOrDefault(),
                entry.State,
                entry.AddedAt,
            })
            .ToArrayAsync(ct);

        return entries
            .Select(entry => new GamesListGameResponse(
                entry.GameId,
                entry.Name,
                entry.CoverSmallUrl,
                entry.CoverLargeUrl,
                entry.GameType,
                GamesListResponseFactory.CreateState(entry.State),
                entry.AddedAt
            ))
            .ToArray();
    }
}
