using ApesDb.Common;
using ApesDb.Domain;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Boards.UpdateBoard;

public sealed class UpdateBoardEndpoint : Endpoint<UpdateBoardRequest, BoardDetailsResponse>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPictureProcessor _pictureProcessor;

    public UpdateBoardEndpoint(
        ApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        IPictureProcessor pictureProcessor
    )
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
        _pictureProcessor = pictureProcessor;
    }

    public override void Configure()
    {
        Put(ApiRoutes.Boards.ById);
        AllowFileUploads();
        Summary(summary => summary.Summary = "Updates the name or picture of a board.");
    }

    public override async Task HandleAsync(UpdateBoardRequest request, CancellationToken ct)
    {
        var userId = User.GetApesDbUserId();
        var board = await _dbContext
            .Boards.Where(board => board.Id == request.BoardId && board.OwnerUserId == userId)
            .SingleOrDefaultAsync(ct);

        if (board is null)
        {
            await Send.NotFoundAsync(ct);
        }
        else
        {
            byte[]? picture = null;
            var pictureIsValid = true;
            if (request.Picture is not null)
            {
                try
                {
                    await using var stream = request.Picture.OpenReadStream();
                    picture = _pictureProcessor.Process(stream);
                }
                catch (InvalidPictureException exception)
                {
                    AddError(request => request.Picture, exception.Message);
                    pictureIsValid = false;
                }
            }

            if (!pictureIsValid)
            {
                await Send.ErrorsAsync(cancellation: ct);
            }
            else
            {
                if (request.Name is not null)
                {
                    board.Name = request.Name.Trim();
                }

                if (picture is not null)
                {
                    board.Picture = picture;
                }
                else if (request.RemovePicture)
                {
                    board.Picture = null;
                }

                board.UpdatedAt = _dateTimeProvider.UtcNow;
                await _dbContext.SaveChangesAsync(ct);

                var games = await _dbContext
                    .BoardEntries.AsNoTracking()
                    .Where(entry => entry.BoardId == board.Id)
                    .ToBoardGameResponsesAsync(ct);

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
    }
}
