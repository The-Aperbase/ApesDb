using ApesDb.Common;
using ApesDb.Domain;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Boards.AddBoardEntry;

public sealed class AddBoardEntryEndpoint : Endpoint<AddBoardEntryRequest>
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
        var canAccess = await _dbContext.Boards.AnyAsync(
            board => board.Id == request.BoardId && board.OwnerUserId == userId,
            ct
        );
        if (!canAccess)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var gameExists = await _dbContext.Games.AnyAsync(game => game.Id == request.GameId, ct);
        if (!gameExists)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "public"."BoardEntries" ("BoardId", "GameId")
            VALUES ({request.BoardId}, {request.GameId})
            ON CONFLICT ("BoardId", "GameId") DO NOTHING
            """,
            ct
        );
        await _dbContext
            .Boards.Where(board => board.Id == request.BoardId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(board => board.UpdatedAt, _dateTimeProvider.UtcNow), ct);

        await Send.NoContentAsync(ct);
    }
}
