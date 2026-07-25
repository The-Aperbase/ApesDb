using ApesDb.Domain.Entities.Boards;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Boards;

internal static class BoardEntryQueryableExtensions
{
    public static async Task<BoardGameResponse[]> ToBoardGameResponsesAsync(
        this IQueryable<BoardEntry> query,
        CancellationToken ct
    )
    {
        return await query
            .OrderBy(entry => entry.AddedAt)
            .ThenBy(entry => entry.GameId)
            .Select(entry => new BoardGameResponse(
                entry.GameId,
                entry.Game.Name,
                entry.Game.CoverSmallUrl,
                entry.Game.CoverLargeUrl,
                entry.Game.GameType!.Name,
                entry.State.Name,
                entry.AddedAt
            ))
            .ToArrayAsync(ct);
    }
}
