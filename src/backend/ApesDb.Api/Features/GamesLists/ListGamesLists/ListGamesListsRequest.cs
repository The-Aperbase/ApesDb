namespace ApesDb.Api.Features.GamesLists.ListGamesLists;

public sealed class ListGamesListsRequest
{
    public Guid TeamId { get; init; }

    public long? GameId { get; init; }
}
