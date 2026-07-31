using ApesDb.Common;
using ApesDb.Domain;
using ApesDb.Domain.Entities.Boards;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Boards.UpdateBoardEntryState;

public sealed class UpdateBoardEntryStateEndpoint : Endpoint<UpdateBoardEntryStateRequest>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateBoardEntryStateEndpoint(ApplicationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public override void Configure()
    {
        Put(ApiRoutes.Boards.EntryByGame);
        Summary(summary => summary.Summary = "Changes the state of a game on a board.");
    }

    public override async Task HandleAsync(UpdateBoardEntryStateRequest request, CancellationToken ct)
    {
        var userId = User.GetApesDbUserId();
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        var stateId = await _dbContext
            .BoardEntryStates.Where(state => state.Name == request.State)
            .Select(state => (int?)state.Id)
            .SingleOrDefaultAsync(ct);
        if (stateId is null)
        {
            AddError(entry => entry.State, "State is invalid.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

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
            var destinationCount = await _dbContext.BoardEntries.CountAsync(
                boardEntry => boardEntry.BoardId == request.BoardId && boardEntry.StateId == stateId.Value,
                ct
            );
            var maximumPosition = destinationCount;
            if (entry.StateId == stateId.Value)
            {
                maximumPosition--;
            }

            if (request.Position > maximumPosition)
            {
                AddError(boardEntry => boardEntry.Position, "Position is outside the destination state.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (entry.StateId == stateId.Value)
            {
                if (request.Position < entry.Position)
                {
                    await _dbContext
                        .BoardEntries.Where(boardEntry =>
                            boardEntry.BoardId == request.BoardId
                            && boardEntry.StateId == entry.StateId
                            && boardEntry.Position >= request.Position
                            && boardEntry.Position < entry.Position
                        )
                        .ExecuteUpdateAsync(
                            setters =>
                                setters.SetProperty(
                                    boardEntry => boardEntry.Position,
                                    boardEntry => boardEntry.Position + 1
                                ),
                            ct
                        );
                }
                else if (request.Position > entry.Position)
                {
                    await _dbContext
                        .BoardEntries.Where(boardEntry =>
                            boardEntry.BoardId == request.BoardId
                            && boardEntry.StateId == entry.StateId
                            && boardEntry.Position > entry.Position
                            && boardEntry.Position <= request.Position
                        )
                        .ExecuteUpdateAsync(
                            setters =>
                                setters.SetProperty(
                                    boardEntry => boardEntry.Position,
                                    boardEntry => boardEntry.Position - 1
                                ),
                            ct
                        );
                }
            }
            else
            {
                await _dbContext
                    .BoardEntries.Where(boardEntry =>
                        boardEntry.BoardId == request.BoardId
                        && boardEntry.StateId == entry.StateId
                        && boardEntry.Position > entry.Position
                    )
                    .ExecuteUpdateAsync(
                        setters =>
                            setters.SetProperty(
                                boardEntry => boardEntry.Position,
                                boardEntry => boardEntry.Position - 1
                            ),
                        ct
                    );
                await _dbContext
                    .BoardEntries.Where(boardEntry =>
                        boardEntry.BoardId == request.BoardId
                        && boardEntry.StateId == stateId.Value
                        && boardEntry.Position >= request.Position
                    )
                    .ExecuteUpdateAsync(
                        setters =>
                            setters.SetProperty(
                                boardEntry => boardEntry.Position,
                                boardEntry => boardEntry.Position + 1
                            ),
                        ct
                    );
            }

            await _dbContext
                .BoardEntries.Where(boardEntry =>
                    boardEntry.BoardId == request.BoardId && boardEntry.GameId == request.GameId
                )
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(boardEntry => boardEntry.StateId, stateId.Value)
                            .SetProperty(boardEntry => boardEntry.Position, request.Position),
                    ct
                );
            board.UpdatedAt = _dateTimeProvider.UtcNow;
            await _dbContext.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
            await Send.NoContentAsync(ct);
        }
    }
}
