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
        var entries = await query
            .OrderBy(entry => entry.AddedAt)
            .ThenBy(entry => entry.GameId)
            .Select(entry => new
            {
                entry.GameId,
                entry.Game.Name,
                entry.Game.CoverSmallUrl,
                entry.Game.CoverLargeUrl,
                GameType = entry.Game.GameType!.Name,
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
