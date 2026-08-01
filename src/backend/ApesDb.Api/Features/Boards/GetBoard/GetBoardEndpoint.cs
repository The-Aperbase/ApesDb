using ApesDb.Domain;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Boards.GetBoard;

public sealed class GetBoardEndpoint : Endpoint<GetBoardRequest, BoardDetailsResponse>
{
    private readonly ApplicationDbContext _dbContext;

    public GetBoardEndpoint(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Boards.ById);
        Summary(summary => summary.Summary = "Gets a board and its games.");
    }

    public override async Task HandleAsync(GetBoardRequest request, CancellationToken ct)
    {
        var userId = User.GetApesDbUserId();
        var board = await _dbContext
            .BoardsAccessibleTo(userId)
            .AsNoTracking()
            .Where(board => board.Id == request.BoardId)
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
            })
            .SingleOrDefaultAsync(ct);

        if (board is null)
        {
            await Send.NotFoundAsync(ct);
        }
        else
        {
            var games = await _dbContext
                .BoardEntries.AsNoTracking()
                .Where(entry => entry.BoardId == request.BoardId)
                .ToBoardGameResponsesAsync(ct);

            await Send.OkAsync(
                new BoardDetailsResponse(
                    board.Id,
                    board.Name,
                    board.CreatedAt,
                    board.UpdatedAt,
                    BoardPictureResponse.From(board.Picture),
                    new BoardUserResponse(board.OwnerUserId, board.OwnerName, board.OwnerPictureUrl),
                    BoardRoles.From(board.OwnerUserId, userId),
                    games
                ),
                ct
            );
        }
    }
}
