using Microsoft.AspNetCore.Http;

namespace ApesDb.Api.Features.Boards.CreateBoard;

public sealed class CreateBoardRequest
{
    public string Name { get; init; } = string.Empty;

    public IFormFile? Picture { get; init; }
}
