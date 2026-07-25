using Microsoft.AspNetCore.Http;

namespace ApesDb.Api.Features.Boards.UpdateBoard;

public sealed class UpdateBoardRequest
{
    public Guid BoardId { get; init; }

    public string? Name { get; init; }

    public IFormFile? Picture { get; init; }

    public bool RemovePicture { get; init; }
}
