using System.ComponentModel;
using ApesDb.Common;
using ApesDb.Domain;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Boards.ListBoards;

public sealed class ListBoardsEndpoint : Endpoint<ListBoardsRequest, Pagable<BoardSummaryResponse>>
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

        var baseQuery = _dbContext.BoardsAccessibleTo(userId).AsNoTracking();
        var query = baseQuery.WhereContains(request.Search, board => board.Name);
        var total = await baseQuery.CountAsync(ct);
        var filteredTotal = await query.CountAsync(ct);
        var boards = await query
            .SortBy(ListSortDirection.Ascending, board => board.Name.ToLower(), board => board.Name, board => board.Id)
            .Page(request.Page, request.PageSize)
            .Select(board => new
            {
                board.Id,
                board.OwnerUserId,
                OwnerName = board.OwnerUser.Name,
                OwnerPictureUrl = board.OwnerUser.PictureUrl,
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
                BoardPictureResponse.From(board.Picture),
                new BoardUserResponse(board.OwnerUserId, board.OwnerName, board.OwnerPictureUrl),
                BoardRoles.From(board.OwnerUserId, userId),
                board.GameCount,
                board.ContainsGame
            ))
            .ToArray();

        await Send.OkAsync(
            new Pagable<BoardSummaryResponse>(response, total, filteredTotal, request.Page, request.PageSize),
            ct
        );
    }
}
