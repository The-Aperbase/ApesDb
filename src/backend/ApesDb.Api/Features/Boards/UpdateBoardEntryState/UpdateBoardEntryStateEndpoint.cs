using ApesDb.Common;
using ApesDb.Domain;
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
        var state = BoardResponseFactory.ParseState(request.State);
        var updated = await _dbContext
            .BoardEntries.Where(entry =>
                entry.BoardId == request.BoardId
                && entry.GameId == request.GameId
                && _dbContext.Boards.Any(board => board.Id == entry.BoardId && board.OwnerUserId == userId)
            )
            .ExecuteUpdateAsync(setters => setters.SetProperty(entry => entry.State, state), ct);

        if (updated == 0)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await _dbContext
            .Boards.Where(board => board.Id == request.BoardId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(board => board.UpdatedAt, _dateTimeProvider.UtcNow), ct);

        await Send.NoContentAsync(ct);
    }
}
