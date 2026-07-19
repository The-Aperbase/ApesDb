using ApesDb.Common;
using ApesDb.Domain;
using ApesDb.Domain.Entities.Teams;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.GamesLists.UpdateGamesList;

public sealed class UpdateGamesListEndpoint : Endpoint<UpdateGamesListRequest, GamesListDetailsResponse>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IGamesListPictureProcessor _pictureProcessor;

    public UpdateGamesListEndpoint(
        ApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        IGamesListPictureProcessor pictureProcessor
    )
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
        _pictureProcessor = pictureProcessor;
    }

    public override void Configure()
    {
        Put(ApiRoutes.GamesLists.ById);
        AllowFileUploads();
        Summary(summary => summary.Summary = "Updates the name or picture of a games list.");
    }

    public override async Task HandleAsync(UpdateGamesListRequest request, CancellationToken ct)
    {
        var userId = User.GetApesDbUserId();
        var list = await _dbContext
            .GamesLists.Where(list =>
                list.Id == request.ListId
                && list.TeamId == request.TeamId
                && _dbContext.TeamMemberships.Any(membership =>
                    membership.TeamId == list.TeamId
                    && membership.UserId == userId
                    && membership.Status == TeamMembershipStatus.Accepted
                )
            )
            .SingleOrDefaultAsync(ct);

        if (list is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        byte[]? picture = null;
        if (request.Picture is not null)
        {
            try
            {
                await using var stream = request.Picture.OpenReadStream();
                picture = _pictureProcessor.Process(stream);
            }
            catch (InvalidGamesListPictureException exception)
            {
                AddError(request => request.Picture, exception.Message);
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }
        }

        if (request.Name is not null)
        {
            list.Name = request.Name.Trim();
        }

        if (picture is not null)
        {
            list.Picture = picture;
        }
        else if (request.RemovePicture)
        {
            list.Picture = null;
        }

        list.UpdatedAt = _dateTimeProvider.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        var games = await GamesListGameLoader.LoadAsync(_dbContext, list.Id, ct);

        await Send.OkAsync(
            new GamesListDetailsResponse(
                list.Id,
                list.Name,
                list.CreatedAt,
                list.UpdatedAt,
                GamesListResponseFactory.CreatePicture(list.Picture),
                games
            ),
            ct
        );
    }
}
