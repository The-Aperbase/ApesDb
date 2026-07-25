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
            .Boards.AsNoTracking()
            .Where(board => board.Id == request.BoardId && board.OwnerUserId == userId)
            .Select(board => new
            {
                board.Id,
                board.Name,
                board.Picture,
                board.CreatedAt,
                board.UpdatedAt,
            })
            .SingleOrDefaultAsync(ct);

        if (board is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var games = await BoardGameLoader.LoadAsync(_dbContext, request.BoardId, ct);

        await Send.OkAsync(
            new BoardDetailsResponse(
                board.Id,
                board.Name,
                board.CreatedAt,
                board.UpdatedAt,
                BoardResponseFactory.CreatePicture(board.Picture),
                games
            ),
            ct
        );
    }
}
