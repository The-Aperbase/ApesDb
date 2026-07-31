using ApesDb.Common;
using ApesDb.Domain;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Boards.RemoveBoardEntry;

public sealed class RemoveBoardEntryEndpoint : Endpoint<RemoveBoardEntryRequest>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RemoveBoardEntryEndpoint(ApplicationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public override void Configure()
    {
        Delete(ApiRoutes.Boards.EntryByGame);
        Summary(summary => summary.Summary = "Removes a game from a board.");
    }

    public override async Task HandleAsync(RemoveBoardEntryRequest request, CancellationToken ct)
    {
        var userId = User.GetApesDbUserId();
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        var board = await _dbContext.Boards.FindOwnedForUpdateAsync(request.BoardId, userId, ct);
        if (board is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var entry = await _dbContext
            .BoardEntries.Where(entry => entry.BoardId == request.BoardId && entry.GameId == request.GameId)
            .Select(entry => new { entry.StateId, entry.Position })
            .SingleOrDefaultAsync(ct);

        if (entry is null)
        {
            await Send.NotFoundAsync(ct);
        }
        else
        {
            await _dbContext
                .BoardEntries.Where(boardEntry =>
                    boardEntry.BoardId == request.BoardId && boardEntry.GameId == request.GameId
                )
                .ExecuteDeleteAsync(ct);
            await _dbContext
                .BoardEntries.Where(boardEntry =>
                    boardEntry.BoardId == request.BoardId
                    && boardEntry.StateId == entry.StateId
                    && boardEntry.Position > entry.Position
                )
                .ExecuteUpdateAsync(
                    setters =>
                        setters.SetProperty(boardEntry => boardEntry.Position, boardEntry => boardEntry.Position - 1),
                    ct
                );
            board.UpdatedAt = _dateTimeProvider.UtcNow;
            await _dbContext.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
            await Send.NoContentAsync(ct);
        }
    }
}
