using Microsoft.AspNetCore.Http;

namespace ApesDb.Api.Features.GamesLists.CreateGamesList;

public sealed class CreateGamesListRequest
{
    public Guid TeamId { get; init; }

    public string Name { get; init; } = string.Empty;

    public IFormFile? Picture { get; init; }
}
