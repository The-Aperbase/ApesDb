namespace ApesDb.Api.Features.Boards.ListBoards;

public sealed class ListBoardsRequest
{
    public long? GameId { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 50;
}
