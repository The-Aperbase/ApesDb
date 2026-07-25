using ApesDb.Api.Features.Boards.GetBoard;
using ApesDb.Common;
using ApesDb.Domain;
using ApesDb.Domain.Entities.Boards;
using FastEndpoints;

namespace ApesDb.Api.Features.Boards.CreateBoard;

public sealed class CreateBoardEndpoint : Endpoint<CreateBoardRequest, BoardSummaryResponse>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPictureProcessor _pictureProcessor;

    public CreateBoardEndpoint(
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
        Post(ApiRoutes.Boards.Create);
        AllowFileUploads();
        Summary(summary => summary.Summary = "Creates a board.");
    }

    public override async Task HandleAsync(CreateBoardRequest request, CancellationToken ct)
    {
        var userId = User.GetApesDbUserId();
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
            var now = _dateTimeProvider.UtcNow;
            var board = new Board
            {
                Id = Guid.CreateVersion7(),
                OwnerUserId = userId,
                Name = request.Name.Trim(),
                Picture = picture,
                CreatedAt = now,
                UpdatedAt = now,
            };

            _dbContext.Boards.Add(board);
            await _dbContext.SaveChangesAsync(ct);

            var response = new BoardSummaryResponse(
                board.Id,
                board.Name,
                board.CreatedAt,
                board.UpdatedAt,
                BoardResponseFactory.CreatePicture(board.Picture),
                0,
                false
            );
            await Send.CreatedAtAsync<GetBoardEndpoint>(new { boardId = board.Id }, response, cancellation: ct);
        }
    }
}
