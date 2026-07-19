namespace ApesDb.Api.Features.GamesLists.GetGamesList;

public sealed class GetGamesListRequest
{
    public Guid TeamId { get; init; }

    public Guid ListId { get; init; }
}
