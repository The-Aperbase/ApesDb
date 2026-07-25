using ApesDb.Domain;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Boards;

internal static class BoardGameLoader
{
    public static async Task<BoardGameResponse[]> LoadAsync(
        ApplicationDbContext dbContext,
        Guid boardId,
        CancellationToken ct
    )
    {
        var entries = await dbContext
            .BoardEntries.AsNoTracking()
            .Where(entry => entry.BoardId == boardId)
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
            .Select(entry => new BoardGameResponse(
                entry.GameId,
                entry.Name,
                entry.CoverSmallUrl,
                entry.CoverLargeUrl,
                entry.GameType,
                BoardResponseFactory.CreateState(entry.State),
                entry.AddedAt
            ))
            .ToArray();
    }
}
