using ApesDb.Domain.Entities.Boards;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Boards;

internal static class BoardEntryQueryableExtensions
{
    private static readonly string[] StateNames = ["todo", "in-progress", "completed", "dnf"];

    public static async Task<Dictionary<string, Dictionary<int, BoardGameResponse>>> ToBoardGameResponsesAsync(
        this IQueryable<BoardEntry> query,
        CancellationToken ct
    )
    {
        var entries = await query
            .OrderBy(entry => entry.StateId)
            .ThenBy(entry => entry.Position)
            .Select(entry => new
            {
                State = entry.State.Name,
                entry.Position,
                Response = new BoardGameResponse(
                    entry.GameId,
                    entry.Game.Name,
                    entry.Game.CoverSmallUrl,
                    entry.Game.CoverLargeUrl,
                    entry.Game.GameType!.Name,
                    entry.AddedAt
                ),
            })
            .ToArrayAsync(ct);

        var games = StateNames.ToDictionary(state => state, _ => new Dictionary<int, BoardGameResponse>());
        foreach (var entry in entries)
        {
            games[entry.State].Add(entry.Position, entry.Response);
        }

        return games;
    }
}
