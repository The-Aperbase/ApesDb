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

        var updated = await _dbContext
            .BoardEntries.Where(entry =>
                entry.BoardId == request.BoardId
                && entry.GameId == request.GameId
                && _dbContext.Boards.Any(board => board.Id == entry.BoardId && board.OwnerUserId == userId)
            )
            .ExecuteUpdateAsync(setters => setters.SetProperty(entry => entry.StateId, stateId.Value), ct);

        if (updated == 0)
        {
            await Send.NotFoundAsync(ct);
        }
        else
        {
            await _dbContext
                .Boards.Where(board => board.Id == request.BoardId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(board => board.UpdatedAt, _dateTimeProvider.UtcNow),
                    ct
                );

            await Send.NoContentAsync(ct);
        }
    }
}
