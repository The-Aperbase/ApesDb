using ApesDb.Domain.Entities.GamesLists;

namespace ApesDb.Api.Features.GamesLists;

public sealed record GamesListPictureResponse(string ContentType, byte[] Data);

public sealed record GamesListSummaryResponse(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    GamesListPictureResponse? Picture,
    int GameCount,
    bool ContainsGame
);

public sealed record GamesListGameResponse(
    long GameId,
    string Name,
    string? CoverSmallUrl,
    string? CoverLargeUrl,
    string? GameType,
    string State,
    DateTime AddedAt
);

public sealed record GamesListDetailsResponse(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    GamesListPictureResponse? Picture,
    GamesListGameResponse[] Games
);

public static class GamesListResponseFactory
{
    public static GamesListPictureResponse? CreatePicture(byte[]? data)
    {
        if (data is null)
        {
            return null;
        }

        return new GamesListPictureResponse("image/webp", data);
    }

    public static string CreateState(GamesListEntryState state)
    {
        if (state == GamesListEntryState.InProgress)
        {
            return "in-progress";
        }

        if (state == GamesListEntryState.Completed)
        {
            return "completed";
        }

        return "todo";
    }

    public static GamesListEntryState ParseState(string state)
    {
        if (state == "in-progress")
        {
            return GamesListEntryState.InProgress;
        }

        if (state == "completed")
        {
            return GamesListEntryState.Completed;
        }

        return GamesListEntryState.Todo;
    }
}
