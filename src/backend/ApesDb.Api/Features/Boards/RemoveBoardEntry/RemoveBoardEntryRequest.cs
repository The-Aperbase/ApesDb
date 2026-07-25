namespace ApesDb.Api.Features.Boards.RemoveBoardEntry;

public sealed class RemoveBoardEntryRequest
{
    public Guid BoardId { get; init; }

    public long GameId { get; init; }
}
