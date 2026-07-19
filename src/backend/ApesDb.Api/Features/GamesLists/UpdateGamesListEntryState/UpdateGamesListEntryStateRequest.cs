namespace ApesDb.Api.Features.GamesLists.UpdateGamesListEntryState;

public sealed class UpdateGamesListEntryStateRequest
{
    public Guid TeamId { get; init; }

    public Guid ListId { get; init; }

    public long GameId { get; init; }

    public string State { get; init; } = string.Empty;
}
