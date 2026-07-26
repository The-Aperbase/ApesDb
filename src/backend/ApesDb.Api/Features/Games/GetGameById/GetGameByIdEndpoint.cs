using System.ComponentModel;
using ApesDb.Common;
using ApesDb.Domain;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace ApesDb.Api.Features.Games.GetGameById;

public sealed class GetGameByIdEndpoint : Endpoint<GetGameByIdRequest, GetGameByIdResponse>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IFusionCache _cache;

    public GetGameByIdEndpoint(ApplicationDbContext dbContext, IFusionCacheProvider cacheProvider)
    {
        _dbContext = dbContext;
        _cache = cacheProvider.GetCache(GameCache.CacheName);
    }

    public override void Configure()
    {
        Get(ApiRoutes.Games.ById);
        Summary(summary =>
        {
            summary.Summary = "Gets a synchronized game by ID.";
            summary.Description = "Returns all client-facing catalog data stored for the requested game.";
        });
    }

    public override async Task HandleAsync(GetGameByIdRequest request, CancellationToken ct)
    {
        var requestedGameIsBase = _dbContext
            .Games.AsNoTracking()
            .Where(game => game.Id == request.Id && game.VersionParentId == null)
            .Select(game => game.Id);
        var gameTypes = _dbContext.GameTypes.AsNoTracking();
        var gameStatuses = _dbContext.GameStatuses.AsNoTracking();
        var covers = _dbContext
            .Games.AsNoTracking()
            .Where(game =>
                game.Id == request.Id
                && (
                    game.CoverImageId != null
                    || game.CoverWidth != null
                    || game.CoverHeight != null
                    || game.CoverSmallUrl != null
                    || game.CoverLargeUrl != null
                )
            )
            .Select(game => new GameCoverResponse(
                game.CoverImageId,
                game.CoverWidth,
                game.CoverHeight,
                game.CoverSmallUrl,
                game.CoverLargeUrl
            ));
        var popularity = _dbContext
            .PopularGames.AsNoTracking()
            .Where(popularGame => popularGame.GameId == request.Id)
            .Select(popularGame => new GamePopularityResponse(
                popularGame.Rank,
                popularGame.SourceRank,
                popularGame.Score,
                new GameReferenceResponse(popularGame.PopularityType.Id, popularGame.PopularityType.Name),
                popularGame.CalculatedAt
            ));
        var gameCompanies = _dbContext.GameCompanies.AsNoTracking().Where(link => link.GameId == request.Id);
        var developers = gameCompanies
            .Where(link => link.Developer)
            .GroupBy(link => new { link.CompanyId, link.Company.Name })
            .SortBy(
                ListSortDirection.Ascending,
                company => company.Key.Name.ToLower(),
                company => company.Key.Name,
                company => company.Key.CompanyId
            )
            .Select(company => new GameReferenceResponse(company.Key.CompanyId, company.Key.Name));
        var publishers = gameCompanies
            .Where(link => link.Publisher)
            .GroupBy(link => new { link.CompanyId, link.Company.Name })
            .SortBy(
                ListSortDirection.Ascending,
                company => company.Key.Name.ToLower(),
                company => company.Key.Name,
                company => company.Key.CompanyId
            )
            .Select(company => new GameReferenceResponse(company.Key.CompanyId, company.Key.Name));
        var portingCompanies = gameCompanies
            .Where(link => link.Porting)
            .GroupBy(link => new { link.CompanyId, link.Company.Name })
            .SortBy(
                ListSortDirection.Ascending,
                company => company.Key.Name.ToLower(),
                company => company.Key.Name,
                company => company.Key.CompanyId
            )
            .Select(company => new GameReferenceResponse(company.Key.CompanyId, company.Key.Name));
        var supportingCompanies = gameCompanies
            .Where(link => link.Supporting)
            .GroupBy(link => new { link.CompanyId, link.Company.Name })
            .SortBy(
                ListSortDirection.Ascending,
                company => company.Key.Name.ToLower(),
                company => company.Key.Name,
                company => company.Key.CompanyId
            )
            .Select(company => new GameReferenceResponse(company.Key.CompanyId, company.Key.Name));
        var platforms = _dbContext.Platforms.AsNoTracking();
        var storePages = _dbContext
            .ExternalGames.AsNoTracking()
            .Where(storePage =>
                storePage.GameId == request.Id
                || (requestedGameIsBase.Any() && storePage.Game.VersionParentId == request.Id)
            )
            .OrderBy(storePage => storePage.GameId != request.Id)
            .ThenBy(storePage => storePage.Game.FirstReleaseDate == null)
            .ThenByDescending(storePage => storePage.Game.FirstReleaseDate)
            .ThenBy(storePage => storePage.Url == null || !storePage.Url.StartsWith("https://"))
            .ThenBy(storePage =>
                storePage.Url == null || (!storePage.Url.StartsWith("https://") && !storePage.Url.StartsWith("http://"))
            )
            .ThenBy(storePage => storePage.ExternalGameSource.Name)
            .ThenBy(storePage => storePage.Id)
            .Select(storePage => new GameStorePageResponse(
                storePage.Id,
                new GameReferenceResponse(storePage.ExternalGameSource.Id, storePage.ExternalGameSource.Name),
                platforms
                    .Where(platform => platform.Id == storePage.PlatformId)
                    .Select(platform => new GameReferenceResponse(platform.Id, platform.Name))
                    .FirstOrDefault(),
                storePage.ExternalId,
                storePage.Name,
                storePage.Url,
                storePage.Year
            ));
        var editions = _dbContext
            .Games.AsNoTracking()
            .Where(edition => requestedGameIsBase.Any() && edition.VersionParentId == request.Id)
            .OrderBy(edition => edition.FirstReleaseDate == null)
            .ThenByDescending(edition => edition.FirstReleaseDate)
            .ThenBy(edition => edition.Name.ToLower())
            .ThenBy(edition => edition.Name)
            .ThenBy(edition => edition.Id)
            .Select(edition => new GameEditionResponse(
                edition.Id,
                edition.Name,
                edition.Summary,
                edition.FirstReleaseDate,
                edition.CoverSmallUrl,
                edition.CoverLargeUrl
            ));
        var addons = _dbContext
            .GameRelations.AsNoTracking()
            .Where(relation => relation.GameId == request.Id)
            .SortBy(
                ListSortDirection.Ascending,
                relation => relation.RelationType,
                relation => relation.RelatedGame.Name,
                relation => relation.RelatedGame.Id
            )
            .Select(relation => new GameAddonResponse(
                relation.RelationType.ToString(),
                relation.RelatedGameId,
                relation.RelatedGame.Name,
                relation.RelatedGame.Summary,
                relation.RelatedGame.FirstReleaseDate,
                relation.RelatedGame.CoverSmallUrl,
                relation.RelatedGame.CoverLargeUrl
            ));
        var genres = _dbContext
            .GameGenres.AsNoTracking()
            .Where(link => link.GameId == request.Id)
            .SortBy(
                ListSortDirection.Ascending,
                link => link.Genre.Name.ToLower(),
                link => link.Genre.Name,
                link => link.GenreId
            )
            .Select(link => new GameReferenceResponse(link.GenreId, link.Genre.Name));
        var themes = _dbContext
            .GameThemes.AsNoTracking()
            .Where(link => link.GameId == request.Id)
            .SortBy(
                ListSortDirection.Ascending,
                link => link.Theme.Name.ToLower(),
                link => link.Theme.Name,
                link => link.ThemeId
            )
            .Select(link => new GameReferenceResponse(link.ThemeId, link.Theme.Name));
        var gameModes = _dbContext
            .GameGameModes.AsNoTracking()
            .Where(link => link.GameId == request.Id)
            .SortBy(
                ListSortDirection.Ascending,
                link => link.GameMode.Name.ToLower(),
                link => link.GameMode.Name,
                link => link.GameModeId
            )
            .Select(link => new GameReferenceResponse(link.GameModeId, link.GameMode.Name));
        var gameEngines = _dbContext
            .GameGameEngines.AsNoTracking()
            .Where(link => link.GameId == request.Id)
            .SortBy(
                ListSortDirection.Ascending,
                link => link.GameEngine.Name.ToLower(),
                link => link.GameEngine.Name,
                link => link.GameEngineId
            )
            .Select(link => new GameReferenceResponse(link.GameEngineId, link.GameEngine.Name));
        var playerPerspectives = _dbContext
            .GamePlayerPerspectives.AsNoTracking()
            .Where(link => link.GameId == request.Id)
            .SortBy(
                ListSortDirection.Ascending,
                link => link.PlayerPerspective.Name.ToLower(),
                link => link.PlayerPerspective.Name,
                link => link.PlayerPerspectiveId
            )
            .Select(link => new GameReferenceResponse(link.PlayerPerspectiveId, link.PlayerPerspective.Name));
        var gamePlatforms = _dbContext
            .GamePlatforms.AsNoTracking()
            .Where(link => link.GameId == request.Id)
            .SortBy(
                ListSortDirection.Ascending,
                link => link.Platform.Name.ToLower(),
                link => link.Platform.Name,
                link => link.PlatformId
            )
            .Select(link => new GameReferenceResponse(link.PlatformId, link.Platform.Name));
        var collections = _dbContext
            .GameCollections.AsNoTracking()
            .Where(link => link.GameId == request.Id)
            .SortBy(
                ListSortDirection.Ascending,
                link => link.Collection.Name.ToLower(),
                link => link.Collection.Name,
                link => link.CollectionId
            )
            .Select(link => new GameReferenceResponse(link.CollectionId, link.Collection.Name));
        var franchises = _dbContext
            .GameFranchises.AsNoTracking()
            .Where(link => link.GameId == request.Id)
            .SortBy(
                ListSortDirection.Ascending,
                link => link.Franchise.Name.ToLower(),
                link => link.Franchise.Name,
                link => link.FranchiseId
            )
            .Select(link => new GameReferenceResponse(link.FranchiseId, link.Franchise.Name));

        var gameQuery = _dbContext
            .Games.AsNoTracking()
            .AsSplitQuery()
            .Where(game => game.Id == request.Id)
            .Select(game => new GetGameByIdResponse(
                game.Id,
                game.Name,
                game.Slug,
                game.Summary,
                game.Storyline,
                game.FirstReleaseDate,
                game.TotalRating,
                game.TotalRatingCount,
                game.IgdbUrl,
                gameTypes
                    .Where(gameType => gameType.Id == game.GameTypeId)
                    .Select(gameType => new GameReferenceResponse(gameType.Id, gameType.Name))
                    .FirstOrDefault(),
                gameStatuses
                    .Where(gameStatus => gameStatus.Id == game.GameStatusId)
                    .Select(gameStatus => new GameReferenceResponse(gameStatus.Id, gameStatus.Name))
                    .FirstOrDefault(),
                game.VersionParentId,
                covers.FirstOrDefault(),
                popularity.FirstOrDefault(),
                developers.ToArray(),
                publishers.ToArray(),
                portingCompanies.ToArray(),
                supportingCompanies.ToArray(),
                storePages.ToArray(),
                editions.ToArray(),
                addons.ToArray(),
                genres.ToArray(),
                themes.ToArray(),
                gameModes.ToArray(),
                gameEngines.ToArray(),
                playerPerspectives.ToArray(),
                gamePlatforms.ToArray(),
                collections.ToArray(),
                franchises.ToArray()
            ));
        var game = await _cache.GetOrSetAsync($"game:v2:{request.Id}", gameQuery.SingleOrDefaultAsync, token: ct);

        if (game is null)
        {
            await Send.NotFoundAsync(ct);
        }
        else
        {
            await Send.OkAsync(game, ct);
        }
    }
}
