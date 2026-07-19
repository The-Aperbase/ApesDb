using Microsoft.AspNetCore.Http;

namespace ApesDb.Api.Features.GamesLists.UpdateGamesList;

public sealed class UpdateGamesListRequest
{
    public Guid TeamId { get; init; }

    public Guid ListId { get; init; }

    public string? Name { get; init; }

    public IFormFile? Picture { get; init; }

    public bool RemovePicture { get; init; }
}
