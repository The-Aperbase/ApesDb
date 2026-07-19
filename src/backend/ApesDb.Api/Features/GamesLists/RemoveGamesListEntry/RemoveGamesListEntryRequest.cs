namespace ApesDb.Api.Features.GamesLists.RemoveGamesListEntry;

public sealed class RemoveGamesListEntryRequest
{
    public Guid TeamId { get; init; }

    public Guid ListId { get; init; }

    public long GameId { get; init; }
}
