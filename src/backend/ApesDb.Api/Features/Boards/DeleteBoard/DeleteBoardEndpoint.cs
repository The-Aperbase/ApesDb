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
        var board = await _dbContext.Boards.FindOwnedForUpdateAsync(request.BoardId, userId, ct);
        if (board is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await _dbContext.BoardEntries.Where(entry => entry.BoardId == request.BoardId).ExecuteDeleteAsync(ct);

        await _dbContext.Boards.Where(existingBoard => existingBoard.Id == board.Id).ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
