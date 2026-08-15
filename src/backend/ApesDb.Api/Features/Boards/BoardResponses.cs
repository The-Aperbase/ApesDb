namespace ApesDb.Api.Features.Boards;

public static class BoardRoles
{
    public const string Owner = "owner";
    public const string Collaborator = "collaborator";

    public static string From(Guid ownerUserId, Guid currentUserId)
    {
        if (ownerUserId == currentUserId)
        {
            return Owner;
        }

        return Collaborator;
    }
}

public sealed record BoardUserResponse(Guid Id, string Name, string? PictureUrl);

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
    BoardUserResponse Owner,
    string Role,
    int GameCount,
    bool ContainsGame
);

public sealed record BoardGameResponse(
    long GameId,
    string Name,
    string? CoverSmallUrl,
    string? CoverLargeUrl,
    string? GameType,
    DateTime AddedAt
);

public sealed record BoardDetailsResponse(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    BoardPictureResponse? Picture,
    BoardUserResponse Owner,
    string Role,
    Dictionary<string, Dictionary<int, BoardGameResponse>> Games
);
