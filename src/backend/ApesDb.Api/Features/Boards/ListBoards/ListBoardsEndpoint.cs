using ApesDb.Domain;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Boards.ListBoards;

public sealed class ListBoardsEndpoint : Endpoint<ListBoardsRequest, BoardSummaryResponse[]>
{
    private readonly ApplicationDbContext _dbContext;

    public ListBoardsEndpoint(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Boards.List);
        Summary(summary =>
        {
            summary.Summary = "Lists the current user's boards.";
            summary.Description =
                "When a game id is provided, each board reports whether it already contains that game.";
        });
    }

    public override async Task HandleAsync(ListBoardsRequest request, CancellationToken ct)
    {
        var userId = User.GetApesDbUserId();

        var boards = await _dbContext
            .Boards.AsNoTracking()
            .Where(board => board.OwnerUserId == userId)
            .OrderBy(board => board.Name.ToLower())
            .ThenBy(board => board.Name)
            .ThenBy(board => board.Id)
            .Select(board => new
            {
                board.Id,
                board.Name,
                board.Picture,
                board.CreatedAt,
                board.UpdatedAt,
                GameCount = board.Entries.Count,
                ContainsGame = request.GameId != null && board.Entries.Any(entry => entry.GameId == request.GameId),
            })
            .ToArrayAsync(ct);

        var response = boards
            .Select(board => new BoardSummaryResponse(
                board.Id,
                board.Name,
                board.CreatedAt,
                board.UpdatedAt,
                BoardResponseFactory.CreatePicture(board.Picture),
                board.GameCount,
                board.ContainsGame
            ))
            .ToArray();

        await Send.OkAsync(response, ct);
    }
}
