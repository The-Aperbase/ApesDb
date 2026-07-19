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
}
