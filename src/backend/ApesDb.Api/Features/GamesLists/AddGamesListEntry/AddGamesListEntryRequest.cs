namespace ApesDb.Api.Features.GamesLists.AddGamesListEntry;

public sealed class AddGamesListEntryRequest
{
    public Guid TeamId { get; init; }

    public Guid ListId { get; init; }

    public long GameId { get; init; }
}
