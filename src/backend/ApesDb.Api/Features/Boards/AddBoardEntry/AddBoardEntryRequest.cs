namespace ApesDb.Api.Features.Boards.AddBoardEntry;

public sealed class AddBoardEntryRequest
{
    public Guid BoardId { get; init; }

    public long GameId { get; init; }
}
