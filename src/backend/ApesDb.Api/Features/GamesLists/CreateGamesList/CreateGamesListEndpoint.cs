using ApesDb.Common;
using ApesDb.Domain;
using ApesDb.Domain.Entities.GamesLists;
using ApesDb.Domain.Entities.Teams;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.GamesLists.CreateGamesList;

public sealed class CreateGamesListEndpoint : Endpoint<CreateGamesListRequest, GamesListSummaryResponse>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IGamesListPictureProcessor _pictureProcessor;

    public CreateGamesListEndpoint(
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
        Post(ApiRoutes.GamesLists.Create);
        AllowFileUploads();
        Summary(summary => summary.Summary = "Creates a games list for a team.");
    }

    public override async Task HandleAsync(CreateGamesListRequest request, CancellationToken ct)
    {
        var userId = User.GetApesDbUserId();
        var isMember = await _dbContext.TeamMemberships.AnyAsync(
            membership =>
                membership.TeamId == request.TeamId
                && membership.UserId == userId
                && membership.Status == TeamMembershipStatus.Accepted,
            ct
        );
        if (!isMember)
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

        var now = _dateTimeProvider.UtcNow;
        var gamesList = new GamesList
        {
            Id = Guid.CreateVersion7(),
            TeamId = request.TeamId,
            Name = request.Name.Trim(),
            Picture = picture,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _dbContext.GamesLists.Add(gamesList);
        await _dbContext.SaveChangesAsync(ct);

        var response = new GamesListSummaryResponse(
            gamesList.Id,
            gamesList.Name,
            gamesList.CreatedAt,
            gamesList.UpdatedAt,
            GamesListResponseFactory.CreatePicture(gamesList.Picture),
            0,
            false
        );
        HttpContext.Response.Headers.Location =
            $"/{ApiRoutes.Api.Prefix}/teams/{gamesList.TeamId}/games-lists/{gamesList.Id}";
        await Send.ResponseAsync(response, StatusCodes.Status201Created, ct);
    }
}
