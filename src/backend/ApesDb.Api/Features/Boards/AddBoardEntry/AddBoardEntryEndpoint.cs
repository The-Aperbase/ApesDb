using ApesDb.Common;
using ApesDb.Domain;
using ApesDb.Domain.Entities.Boards;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Boards.AddBoardEntry;

public sealed class AddBoardEntryEndpoint : Endpoint<AddBoardEntryRequest, AddBoardEntryResponse>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddBoardEntryEndpoint(ApplicationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Boards.Entries);
        Summary(summary => summary.Summary = "Adds a game to a board.");
    }

    public override async Task HandleAsync(AddBoardEntryRequest request, CancellationToken ct)
    {
        var userId = User.GetApesDbUserId();
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        var board = await _dbContext.Boards.FindAccessibleForUpdateAsync(request.BoardId, userId, ct);
        if (board is null)
        {
            await Send.NotFoundAsync(ct);
        }
        else
        {
            var gameExists = await _dbContext.Games.AnyAsync(game => game.Id == request.GameId, ct);
            if (!gameExists)
            {
                await Send.NotFoundAsync(ct);
            }
            else
            {
                var entryExists = await _dbContext.BoardEntries.AnyAsync(
                    entry => entry.BoardId == request.BoardId && entry.GameId == request.GameId,
                    ct
                );
                if (!entryExists)
                {
                    var todoStateId = await _dbContext
                        .BoardEntryStates.Where(state => state.Name == "todo")
                        .Select(state => state.Id)
                        .SingleAsync(ct);
                    var position = await _dbContext.BoardEntries.CountAsync(
                        entry => entry.BoardId == request.BoardId && entry.StateId == todoStateId,
                        ct
                    );
                    _dbContext.BoardEntries.Add(
                        new BoardEntry
                        {
                            BoardId = request.BoardId,
                            GameId = request.GameId,
                            StateId = todoStateId,
                            Position = position,
                        }
                    );
                    board.UpdatedAt = _dateTimeProvider.UtcNow;
                    await _dbContext.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                }

                await Send.OkAsync(new AddBoardEntryResponse(request.GameId), ct);
            }
        }
    }
}
