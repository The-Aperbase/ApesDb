namespace ApesDb.Api.Features.Boards;

public sealed record BoardPictureResponse(string ContentType, byte[] Data)
{
    public static BoardPictureResponse? From(byte[]? data)
    {
        if (data is null)
        {
            return null;
        }

        return new BoardPictureResponse("image/webp", data);
    }
}

public sealed record BoardSummaryResponse(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    BoardPictureResponse? Picture,
    int GameCount,
    bool ContainsGame
);

public sealed record BoardGameResponse(
    long GameId,
    string Name,
    string? CoverSmallUrl,
    string? CoverLargeUrl,
    string? GameType,
    string State,
    DateTime AddedAt
);

public sealed record BoardDetailsResponse(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    BoardPictureResponse? Picture,
    BoardGameResponse[] Games
);
