using ApesDb.Domain.Entities.Boards;

namespace ApesDb.Api.Features.Boards;

public sealed record BoardPictureResponse(string ContentType, byte[] Data);

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

public static class BoardResponseFactory
{
    public static BoardPictureResponse? CreatePicture(byte[]? data)
    {
        if (data is null)
        {
            return null;
        }

        return new BoardPictureResponse("image/webp", data);
    }

    public static string CreateState(BoardEntryState state)
    {
        if (state == BoardEntryState.InProgress)
        {
            return "in-progress";
        }

        if (state == BoardEntryState.Completed)
        {
            return "completed";
        }

        if (state == BoardEntryState.Dnf)
        {
            return "dnf";
        }

        return "todo";
    }

    public static BoardEntryState ParseState(string state)
    {
        if (state == "in-progress")
        {
            return BoardEntryState.InProgress;
        }

        if (state == "completed")
        {
            return BoardEntryState.Completed;
        }

        if (state == "dnf")
        {
            return BoardEntryState.Dnf;
        }

        return BoardEntryState.Todo;
    }
}
