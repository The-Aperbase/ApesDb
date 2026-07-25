using ApesDb.Domain.Entities.Boards;
using ApesDb.Domain.Entities.Games;

namespace ApesDb.Api.Tests.TestData;

public static class BoardEntryTestData
{
    public const long BacklogGameId = 11156L;
    public const long CompletedGameId = 492L;
    public const long AddableGameId = 534L;

    public static BoardEntry[] Create(IReadOnlyDictionary<Guid, Board> boards, IReadOnlyDictionary<long, Game> games)
    {
        return
        [
            new BoardEntry
            {
                BoardId = BoardTestData.BacklogId,
                Board = boards[BoardTestData.BacklogId],
                GameId = BacklogGameId,
                Game = games[BacklogGameId],
                State = BoardEntryState.InProgress,
                AddedAt = new DateTime(2026, 1, 12, 8, 0, 0, DateTimeKind.Utc),
            },
            new BoardEntry
            {
                BoardId = BoardTestData.BacklogId,
                Board = boards[BoardTestData.BacklogId],
                GameId = CompletedGameId,
                Game = games[CompletedGameId],
                State = BoardEntryState.Completed,
                AddedAt = new DateTime(2026, 1, 13, 8, 0, 0, DateTimeKind.Utc),
            },
        ];
    }
}
