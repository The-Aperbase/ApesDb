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
        var deleted = await _dbContext
            .BoardEntries.Where(entry =>
                entry.BoardId == request.BoardId
                && entry.GameId == request.GameId
                && _dbContext.Boards.Any(board => board.Id == entry.BoardId && board.OwnerUserId == userId)
            )
            .ExecuteDeleteAsync(ct);

        if (deleted == 0)
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
