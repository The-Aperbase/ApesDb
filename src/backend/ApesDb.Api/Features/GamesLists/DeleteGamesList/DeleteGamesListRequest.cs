namespace ApesDb.Api.Features.GamesLists.DeleteGamesList;

public sealed class DeleteGamesListRequest
{
    public Guid TeamId { get; init; }

    public Guid ListId { get; init; }
}
