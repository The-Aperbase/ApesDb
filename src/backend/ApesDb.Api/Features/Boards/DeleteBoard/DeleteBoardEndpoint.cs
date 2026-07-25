using ApesDb.Domain;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Boards.DeleteBoard;

public sealed class DeleteBoardEndpoint : Endpoint<DeleteBoardRequest>
{
    private readonly ApplicationDbContext _dbContext;

    public DeleteBoardEndpoint(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Delete(ApiRoutes.Boards.ById);
        Summary(summary => summary.Summary = "Deletes a board and its entries.");
    }

    public override async Task HandleAsync(DeleteBoardRequest request, CancellationToken ct)
    {
        var userId = User.GetApesDbUserId();
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        await _dbContext
            .BoardEntries.Where(entry => entry.BoardId == request.BoardId && entry.Board.OwnerUserId == userId)
            .ExecuteDeleteAsync(ct);

        var deleted = await _dbContext
            .Boards.Where(board => board.Id == request.BoardId && board.OwnerUserId == userId)
            .ExecuteDeleteAsync(ct);

        if (deleted == 0)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await transaction.CommitAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
