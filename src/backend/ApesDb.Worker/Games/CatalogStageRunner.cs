using ApesDb.Common;
using ApesDb.Domain;
using ApesDb.Domain.Entities.Games;
using ApesDb.Domain.Entities.IgdbSync;
using ApesDb.Igdb.Sdk;
using ApesDb.Igdb.Sdk.Models;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Worker.Games;

public sealed class CatalogStageRunner : ICatalogStageRunner
{
    private const int PageSize = 500;
    private const int FinalRetryCount = 3;

    private static readonly BulkConfig UpsertConfig = new()
    {
        PropertiesToExcludeOnUpdate = [nameof(IIgdbEntity.CreatedAt)],
    };

    private static readonly BulkConfig SkippedRowUpsertConfig = new()
    {
        PropertiesToExcludeOnUpdate = [nameof(IgdbSyncSkippedRow.CreatedAt)],
    };

    private readonly ApplicationDbContext _dbContext;
    private readonly IIgdbService _igdbService;
    private readonly IPopularitySynchronizer _popularitySynchronizer;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CatalogStageRunner> _logger;

    public CatalogStageRunner(
        ApplicationDbContext dbContext,
        IIgdbService igdbService,
        IPopularitySynchronizer popularitySynchronizer,
        IDateTimeProvider dateTimeProvider,
        ILogger<CatalogStageRunner> logger
    )
    {
        _dbContext = dbContext;
        _igdbService = igdbService;
        _popularitySynchronizer = popularitySynchronizer;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task RunAsync(
        Guid runId,
        IgdbSyncStageKind stageKind,
        int retryCount,
        CancellationToken cancellationToken = default
    )
    {
        var (run, stage) = await LoadStageAsync(runId, stageKind, cancellationToken);
        if (stage.Status == IgdbSyncStageStatus.Succeeded)
        {
            return;
        }

        if (
            await _dbContext.IgdbSyncStages.AnyAsync(
                value =>
                    value.RunId == runId && value.Order < stage.Order && value.Status != IgdbSyncStageStatus.Succeeded,
                cancellationToken
            )
        )
        {
            throw new InvalidOperationException(
                $"IGDB stage {stageKind} cannot run before every earlier stage in run {runId} succeeds."
            );
        }

        var startedAt = _dateTimeProvider.UtcNow;
        run.Status = IgdbSyncRunStatus.Running;
        run.StartedAt ??= startedAt;
        run.UpdatedAt = startedAt;
        run.Error = null;
        stage.Status = IgdbSyncStageStatus.Running;
        stage.StartedAt ??= startedAt;
        stage.UpdatedAt = startedAt;
        stage.Error = null;
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await RunCoreAsync(run, stage, cancellationToken);
        }
        catch (Exception exception)
        {
            await RecordFailureAsync(runId, stageKind, retryCount, exception, cancellationToken);
            throw;
        }
    }

    private async Task RunCoreAsync(IgdbSyncRun run, IgdbSyncStage stage, CancellationToken cancellationToken)
    {
        var window = CreateWindow(run);
        switch (stage.Kind)
        {
            case IgdbSyncStageKind.GameTypes:
                await SyncEntitiesAsync(
                    run,
                    stage,
                    _igdbService.FetchGameTypesPageAsync,
                    value => value.Id,
                    (value, now) =>
                        new GameType
                        {
                            Id = value.Id,
                            Name = Required(value.Name, nameof(IgdbGameType), value.Id),
                            Checksum = value.Checksum,
                            IgdbUpdatedAt = ToUtcDateTime(value.UpdatedAt),
                            CreatedAt = now,
                            UpdatedAt = now,
                            LastSyncedAt = now,
                        },
                    window,
                    cancellationToken
                );
                break;
            case IgdbSyncStageKind.GameStatuses:
                await SyncEntitiesAsync(
                    run,
                    stage,
                    _igdbService.FetchGameStatusesPageAsync,
                    value => value.Id,
                    (value, now) =>
                        new GameStatus
                        {
                            Id = value.Id,
                            Name = Required(value.Name, nameof(IgdbGameStatus), value.Id),
                            Checksum = value.Checksum,
                            IgdbUpdatedAt = ToUtcDateTime(value.UpdatedAt),
                            CreatedAt = now,
                            UpdatedAt = now,
                            LastSyncedAt = now,
                        },
                    window,
                    cancellationToken
                );
                break;
            case IgdbSyncStageKind.Genres:
                await SyncEntitiesAsync(
                    run,
                    stage,
                    _igdbService.FetchGenresPageAsync,
                    value => value.Id,
                    (value, now) =>
                        new Genre
                        {
                            Id = value.Id,
                            Name = Required(value.Name, nameof(IgdbGenre), value.Id),
                            Slug = value.Slug,
                            IgdbUrl = value.Url,
                            Checksum = value.Checksum,
                            IgdbUpdatedAt = ToUtcDateTime(value.UpdatedAt),
                            CreatedAt = now,
                            UpdatedAt = now,
                            LastSyncedAt = now,
                        },
                    window,
                    cancellationToken
                );
                break;
            case IgdbSyncStageKind.Themes:
                await SyncEntitiesAsync(
                    run,
                    stage,
                    _igdbService.FetchThemesPageAsync,
                    value => value.Id,
                    (value, now) =>
                        new Theme
                        {
                            Id = value.Id,
                            Name = Required(value.Name, nameof(IgdbTheme), value.Id),
                            Slug = value.Slug,
                            IgdbUrl = value.Url,
                            Checksum = value.Checksum,
                            IgdbUpdatedAt = ToUtcDateTime(value.UpdatedAt),
                            CreatedAt = now,
                            UpdatedAt = now,
                            LastSyncedAt = now,
                        },
                    window,
                    cancellationToken
                );
                break;
            case IgdbSyncStageKind.GameModes:
                await SyncEntitiesAsync(
                    run,
                    stage,
                    _igdbService.FetchGameModesPageAsync,
                    value => value.Id,
                    (value, now) =>
                        new GameMode
                        {
                            Id = value.Id,
                            Name = Required(value.Name, nameof(IgdbGameMode), value.Id),
                            Slug = value.Slug,
                            IgdbUrl = value.Url,
                            Checksum = value.Checksum,
                            IgdbUpdatedAt = ToUtcDateTime(value.UpdatedAt),
                            CreatedAt = now,
                            UpdatedAt = now,
                            LastSyncedAt = now,
                        },
                    window,
                    cancellationToken
                );
                break;
            case IgdbSyncStageKind.GameEngines:
                await SyncEntitiesAsync(
                    run,
                    stage,
                    _igdbService.FetchGameEnginesPageAsync,
                    value => value.Id,
                    (value, now) =>
                        new GameEngine
                        {
                            Id = value.Id,
                            Name = Required(value.Name, nameof(IgdbGameEngine), value.Id),
                            Checksum = value.Checksum,
                            IgdbUpdatedAt = ToUtcDateTime(value.UpdatedAt),
                            CreatedAt = now,
                            UpdatedAt = now,
                            LastSyncedAt = now,
                        },
                    window,
                    cancellationToken
                );
                break;
            case IgdbSyncStageKind.PlayerPerspectives:
                await SyncEntitiesAsync(
                    run,
                    stage,
                    _igdbService.FetchPlayerPerspectivesPageAsync,
                    value => value.Id,
                    (value, now) =>
                        new PlayerPerspective
                        {
                            Id = value.Id,
                            Name = Required(value.Name, nameof(IgdbPlayerPerspective), value.Id),
                            Slug = value.Slug,
                            IgdbUrl = value.Url,
                            Checksum = value.Checksum,
                            IgdbUpdatedAt = ToUtcDateTime(value.UpdatedAt),
                            CreatedAt = now,
                            UpdatedAt = now,
                            LastSyncedAt = now,
                        },
                    window,
                    cancellationToken
                );
                break;
            case IgdbSyncStageKind.PlatformTypes:
                await SyncEntitiesAsync(
                    run,
                    stage,
                    _igdbService.FetchPlatformTypesPageAsync,
                    value => value.Id,
                    (value, now) =>
                        new PlatformType
                        {
                            Id = value.Id,
                            Name = Required(value.Name, nameof(IgdbPlatformType), value.Id),
                            Checksum = value.Checksum,
                            IgdbUpdatedAt = ToUtcDateTime(value.UpdatedAt),
                            CreatedAt = now,
                            UpdatedAt = now,
                            LastSyncedAt = now,
                        },
                    window,
                    cancellationToken
                );
                break;
            case IgdbSyncStageKind.WebsiteTypes:
                await SyncEntitiesAsync(
                    run,
                    stage,
                    _igdbService.FetchWebsiteTypesPageAsync,
                    value => value.Id,
                    (value, now) =>
                        new WebsiteType
                        {
                            Id = value.Id,
                            Name = Required(value.Name, nameof(IgdbWebsiteType), value.Id),
                            Checksum = value.Checksum,
                            IgdbUpdatedAt = ToUtcDateTime(value.UpdatedAt),
                            CreatedAt = now,
                            UpdatedAt = now,
                            LastSyncedAt = now,
                        },
                    window,
                    cancellationToken
                );
                break;
            case IgdbSyncStageKind.PopularityTypes:
                await SyncEntitiesAsync(
                    run,
                    stage,
                    _igdbService.FetchPopularityTypesPageAsync,
                    value => value.Id,
                    (value, now) =>
                        new PopularityType
                        {
                            Id = value.Id,
                            Name = Required(value.Name, nameof(IgdbPopularityType), value.Id),
                            ExternalPopularitySourceId = value.ExternalPopularitySourceId,
                            Checksum = value.Checksum,
                            IgdbUpdatedAt = ToUtcDateTime(value.UpdatedAt),
                            CreatedAt = now,
                            UpdatedAt = now,
                            LastSyncedAt = now,
                        },
                    window,
                    cancellationToken
                );
                break;
            case IgdbSyncStageKind.ExternalGameSources:
                await SyncEntitiesAsync(
                    run,
                    stage,
                    _igdbService.FetchExternalGameSourcesPageAsync,
                    value => value.Id,
                    (value, now) =>
                        new ExternalGameSource
                        {
                            Id = value.Id,
                            Name = Required(value.Name, nameof(IgdbExternalGameSource), value.Id),
                            Checksum = value.Checksum,
                            IgdbUpdatedAt = ToUtcDateTime(value.UpdatedAt),
                            CreatedAt = now,
                            UpdatedAt = now,
                            LastSyncedAt = now,
                        },
                    window,
                    cancellationToken
                );
                break;
            case IgdbSyncStageKind.Companies:
                await SyncEntitiesAsync(
                    run,
                    stage,
                    _igdbService.FetchCompaniesPageAsync,
                    value => value.Id,
                    (value, now) =>
                        new Company
                        {
                            Id = value.Id,
                            Name = Required(value.Name, nameof(IgdbCompany), value.Id),
                            Slug = value.Slug,
                            Description = value.Description,
                            CountryCode = value.CountryCode,
                            IgdbUrl = value.Url,
                            LogoImageId = value.Logo?.ImageId,
                            LogoWidth = value.Logo?.Width,
                            LogoHeight = value.Logo?.Height,
                            Checksum = value.Checksum,
                            IgdbUpdatedAt = ToUtcDateTime(value.UpdatedAt),
                            CreatedAt = now,
                            UpdatedAt = now,
                            LastSyncedAt = now,
                        },
                    window,
                    cancellationToken
                );
                break;
            case IgdbSyncStageKind.Collections:
                await SyncEntitiesAsync(
                    run,
                    stage,
                    _igdbService.FetchCollectionsPageAsync,
                    value => value.Id,
                    (value, now) =>
                        new Collection
                        {
                            Id = value.Id,
                            Name = Required(value.Name, nameof(IgdbCollection), value.Id),
                            Slug = value.Slug,
                            IgdbUrl = value.Url,
                            Checksum = value.Checksum,
                            IgdbUpdatedAt = ToUtcDateTime(value.UpdatedAt),
                            CreatedAt = now,
                            UpdatedAt = now,
                            LastSyncedAt = now,
                        },
                    window,
                    cancellationToken
                );
                break;
            case IgdbSyncStageKind.Franchises:
                await SyncEntitiesAsync(
                    run,
                    stage,
                    _igdbService.FetchFranchisesPageAsync,
                    value => value.Id,
                    (value, now) =>
                        new Franchise
                        {
                            Id = value.Id,
                            Name = Required(value.Name, nameof(IgdbFranchise), value.Id),
                            Slug = value.Slug,
                            IgdbUrl = value.Url,
                            Checksum = value.Checksum,
                            IgdbUpdatedAt = ToUtcDateTime(value.UpdatedAt),
                            CreatedAt = now,
                            UpdatedAt = now,
                            LastSyncedAt = now,
                        },
                    window,
                    cancellationToken
                );
                break;
            case IgdbSyncStageKind.Platforms:
                await SyncEntitiesAsync(
                    run,
                    stage,
                    _igdbService.FetchPlatformsPageAsync,
                    value => value.Id,
                    (value, now) =>
                        new Platform
                        {
                            Id = value.Id,
                            Name = Required(value.Name, nameof(IgdbPlatform), value.Id),
                            Abbreviation = value.Abbreviation,
                            AlternativeName = value.AlternativeName,
                            Slug = value.Slug,
                            Summary = value.Summary,
                            IgdbUrl = value.Url,
                            PlatformTypeId = value.PlatformTypeId,
                            Generation = value.Generation,
                            LogoImageId = value.Logo?.ImageId,
                            LogoWidth = value.Logo?.Width,
                            LogoHeight = value.Logo?.Height,
                            Checksum = value.Checksum,
                            IgdbUpdatedAt = ToUtcDateTime(value.UpdatedAt),
                            CreatedAt = now,
                            UpdatedAt = now,
                            LastSyncedAt = now,
                        },
                    window,
                    cancellationToken
                );
                break;
            case IgdbSyncStageKind.PlatformLinks:
                await SyncPlatformLinksAsync(run, stage, cancellationToken);
                break;
            case IgdbSyncStageKind.Games:
                await SyncGamesAsync(run, stage, window, cancellationToken);
                break;
            case IgdbSyncStageKind.GameRelations:
                await SyncGameRelationsAsync(run, stage, cancellationToken);
                break;
            case IgdbSyncStageKind.InvolvedCompanies:
                await SyncDependentEntitiesAsync(
                    run,
                    stage,
                    _igdbService.FetchInvolvedCompaniesPageAsync,
                    value => value.Id,
                    PrepareInvolvedCompaniesPageAsync,
                    window,
                    cancellationToken
                );
                break;
            case IgdbSyncStageKind.ExternalGames:
                await SyncDependentEntitiesAsync(
                    run,
                    stage,
                    _igdbService.FetchExternalGamesPageAsync,
                    value => value.Id,
                    PrepareExternalGamesPageAsync,
                    window,
                    cancellationToken
                );
                break;
            case IgdbSyncStageKind.Popularity:
                await _popularitySynchronizer.RefreshAsync(allowDuringCatalogRun: true, cancellationToken);
                await MarkStageSucceededAsync(run, stage, cancellationToken);
                break;
            case IgdbSyncStageKind.Complete:
                throw new InvalidOperationException("The completion stage is handled by the catalog orchestrator.");
            default:
                throw new ArgumentOutOfRangeException(nameof(stage.Kind), stage.Kind, "Unsupported IGDB sync stage.");
        }
    }

    private async Task SyncEntitiesAsync<TSource, TEntity>(
        IgdbSyncRun run,
        IgdbSyncStage stage,
        Func<long, IgdbSyncWindow?, CancellationToken, Task<IReadOnlyList<TSource>>> fetchPage,
        Func<TSource, long> idSelector,
        Func<TSource, DateTime, TEntity> map,
        IgdbSyncWindow? window,
        CancellationToken cancellationToken
    )
        where TEntity : class, IIgdbEntity
    {
        var cursor = stage.PageCursor;
        while (true)
        {
            var page = await fetchPage(cursor, window, cancellationToken);
            if (page.Count == 0)
            {
                break;
            }

            var nextCursor = page.Max(idSelector);
            ValidatePageCursor(cursor, nextCursor, stage.Kind);
            var now = _dateTimeProvider.UtcNow;
            var entities = page.DistinctBy(idSelector).Select(value => map(value, now)).ToList();

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            if (entities.Count > 0)
            {
                await _dbContext.BulkInsertOrUpdateAsync(entities, UpsertConfig, cancellationToken: cancellationToken);
            }

            UpdatePageProgress(run, stage, nextCursor, page.Count, now);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            cursor = nextCursor;
            if (page.Count < PageSize)
            {
                break;
            }
        }

        await MarkStageSucceededAsync(run, stage, cancellationToken);
    }

    private async Task SyncDependentEntitiesAsync<TSource, TEntity>(
        IgdbSyncRun run,
        IgdbSyncStage stage,
        Func<long, IgdbSyncWindow?, CancellationToken, Task<IReadOnlyList<TSource>>> fetchPage,
        Func<TSource, long> idSelector,
        Func<
            Guid,
            IReadOnlyList<TSource>,
            DateTime,
            CancellationToken,
            Task<ValidatedDependentPage<TEntity>>
        > preparePage,
        IgdbSyncWindow? window,
        CancellationToken cancellationToken
    )
        where TEntity : class, IIgdbEntity
    {
        var cursor = stage.PageCursor;
        while (true)
        {
            var page = await fetchPage(cursor, window, cancellationToken);
            if (page.Count == 0)
            {
                break;
            }

            var nextCursor = page.Max(idSelector);
            ValidatePageCursor(cursor, nextCursor, stage.Kind);
            var now = _dateTimeProvider.UtcNow;
            var distinctPage = page.DistinctBy(idSelector).ToArray();
            var preparedPage = await preparePage(stage.Id, distinctPage, now, cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            if (preparedPage.SkippedRows.Count > 0)
            {
                await _dbContext.BulkInsertOrUpdateAsync(
                    preparedPage.SkippedRows,
                    SkippedRowUpsertConfig,
                    cancellationToken: cancellationToken
                );
            }

            if (preparedPage.Entities.Count > 0)
            {
                await _dbContext.BulkInsertOrUpdateAsync(
                    preparedPage.Entities,
                    UpsertConfig,
                    cancellationToken: cancellationToken
                );
            }

            UpdatePageProgress(run, stage, nextCursor, page.Count, now);
            UpdateSkippedProgress(run, stage, preparedPage.SkippedEntityCount);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            LogSkippedRows(run.Id, stage.Kind, preparedPage);
            cursor = nextCursor;
            if (page.Count < PageSize)
            {
                break;
            }
        }

        await MarkStageSucceededAsync(run, stage, cancellationToken);
    }

    private async Task<ValidatedDependentPage<GameCompany>> PrepareInvolvedCompaniesPageAsync(
        Guid stageId,
        IReadOnlyList<IgdbInvolvedCompany> page,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var requestedGameIds = page.Select(value => value.GameId).Distinct().ToArray();
        var requestedCompanyIds = page.Select(value => value.CompanyId).Distinct().ToArray();
        var storedGameIds = await _dbContext
            .Games.AsNoTracking()
            .Where(value => requestedGameIds.Contains(value.Id))
            .Select(value => value.Id)
            .ToHashSetAsync(cancellationToken);
        var storedCompanyIds = await _dbContext
            .Companies.AsNoTracking()
            .Where(value => requestedCompanyIds.Contains(value.Id))
            .Select(value => value.Id)
            .ToHashSetAsync(cancellationToken);
        var entities = new List<GameCompany>(page.Count);
        var skippedRows = new List<IgdbSyncSkippedRow>();
        var skippedEntityCount = 0;

        foreach (var value in page)
        {
            var isSkipped = false;
            if (!storedGameIds.Contains(value.GameId))
            {
                skippedRows.Add(CreateSkippedRow(stageId, value.Id, IgdbSyncSkipReason.MissingGame, value.GameId, now));
                isSkipped = true;
            }

            if (!storedCompanyIds.Contains(value.CompanyId))
            {
                skippedRows.Add(
                    CreateSkippedRow(stageId, value.Id, IgdbSyncSkipReason.MissingCompany, value.CompanyId, now)
                );
                isSkipped = true;
            }

            if (isSkipped)
            {
                skippedEntityCount++;
                continue;
            }

            entities.Add(
                new GameCompany
                {
                    Id = value.Id,
                    GameId = value.GameId,
                    CompanyId = value.CompanyId,
                    Developer = value.Developer,
                    Publisher = value.Publisher,
                    Porting = value.Porting,
                    Supporting = value.Supporting,
                    Checksum = value.Checksum,
                    IgdbUpdatedAt = ToUtcDateTime(value.UpdatedAt),
                    CreatedAt = now,
                    UpdatedAt = now,
                    LastSyncedAt = now,
                }
            );
        }

        return new ValidatedDependentPage<GameCompany>(entities, skippedRows, skippedEntityCount);
    }

    private async Task<ValidatedDependentPage<ExternalGame>> PrepareExternalGamesPageAsync(
        Guid stageId,
        IReadOnlyList<IgdbExternalGame> page,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var requestedGameIds = page.Select(value => value.GameId).Distinct().ToArray();
        var requestedSourceIds = page.Select(value => value.SourceId).Distinct().ToArray();
        var requestedPlatformIds = page.Where(value => value.PlatformId.HasValue)
            .Select(value => value.PlatformId!.Value)
            .Distinct()
            .ToArray();
        var storedGameIds = await _dbContext
            .Games.AsNoTracking()
            .Where(value => requestedGameIds.Contains(value.Id))
            .Select(value => value.Id)
            .ToHashSetAsync(cancellationToken);
        var storedSourceIds = await _dbContext
            .ExternalGameSources.AsNoTracking()
            .Where(value => requestedSourceIds.Contains(value.Id))
            .Select(value => value.Id)
            .ToHashSetAsync(cancellationToken);
        var storedPlatformIds = await _dbContext
            .Platforms.AsNoTracking()
            .Where(value => requestedPlatformIds.Contains(value.Id))
            .Select(value => value.Id)
            .ToHashSetAsync(cancellationToken);
        var entities = new List<ExternalGame>(page.Count);
        var skippedRows = new List<IgdbSyncSkippedRow>();
        var skippedEntityCount = 0;

        foreach (var value in page)
        {
            var isSkipped = false;
            if (!storedGameIds.Contains(value.GameId))
            {
                skippedRows.Add(CreateSkippedRow(stageId, value.Id, IgdbSyncSkipReason.MissingGame, value.GameId, now));
                isSkipped = true;
            }

            if (!storedSourceIds.Contains(value.SourceId))
            {
                skippedRows.Add(
                    CreateSkippedRow(
                        stageId,
                        value.Id,
                        IgdbSyncSkipReason.MissingExternalGameSource,
                        value.SourceId,
                        now
                    )
                );
                isSkipped = true;
            }

            if (value.PlatformId is { } platformId && !storedPlatformIds.Contains(platformId))
            {
                skippedRows.Add(
                    CreateSkippedRow(stageId, value.Id, IgdbSyncSkipReason.MissingPlatform, platformId, now)
                );
                isSkipped = true;
            }

            if (isSkipped)
            {
                skippedEntityCount++;
                continue;
            }

            entities.Add(
                new ExternalGame
                {
                    Id = value.Id,
                    GameId = value.GameId,
                    ExternalGameSourceId = value.SourceId,
                    PlatformId = value.PlatformId,
                    ExternalId = value.Uid,
                    Name = value.Name,
                    Url = value.Url,
                    Year = value.Year,
                    Checksum = value.Checksum,
                    IgdbUpdatedAt = ToUtcDateTime(value.UpdatedAt),
                    CreatedAt = now,
                    UpdatedAt = now,
                    LastSyncedAt = now,
                }
            );
        }

        return new ValidatedDependentPage<ExternalGame>(entities, skippedRows, skippedEntityCount);
    }

    private static IgdbSyncSkippedRow CreateSkippedRow(
        Guid stageId,
        long entityId,
        IgdbSyncSkipReason reason,
        long missingDependencyId,
        DateTime now
    )
    {
        return new IgdbSyncSkippedRow
        {
            StageId = stageId,
            EntityId = entityId,
            Reason = reason,
            MissingDependencyId = missingDependencyId,
            CreatedAt = now,
        };
    }

    private void LogSkippedRows<TEntity>(Guid runId, IgdbSyncStageKind stageKind, ValidatedDependentPage<TEntity> page)
        where TEntity : class
    {
        if (page.SkippedEntityCount == 0)
        {
            return;
        }

        var samples = string.Join(
            ", ",
            page.SkippedRows.Take(10).Select(value => $"{value.EntityId}:{value.Reason}:{value.MissingDependencyId}")
        );
        _logger.LogWarning(
            "Skipped {SkippedRows} IGDB rows containing {MissingDependencies} missing dependencies in stage {Stage} "
                + "for run {RunId}. Samples: {Samples}",
            page.SkippedEntityCount,
            page.SkippedRows.Count,
            stageKind,
            runId,
            samples
        );
    }

    private async Task SyncPlatformLinksAsync(IgdbSyncRun run, IgdbSyncStage stage, CancellationToken cancellationToken)
    {
        var storedPlatformIds = await _dbContext
            .Platforms.AsNoTracking()
            .Select(value => value.Id)
            .ToHashSetAsync(cancellationToken);
        var storedWebsiteTypeIds = await _dbContext
            .WebsiteTypes.AsNoTracking()
            .Select(value => value.Id)
            .ToHashSetAsync(cancellationToken);
        var websitePlatforms = new Dictionary<long, long>();
        var platformCursor = -1L;
        while (true)
        {
            var platforms = await _igdbService.FetchPlatformsPageAsync(platformCursor, null, cancellationToken);
            if (platforms.Count == 0)
            {
                break;
            }

            var nextCursor = platforms.Max(value => value.Id);
            ValidatePageCursor(platformCursor, nextCursor, stage.Kind);
            foreach (var platform in platforms)
            {
                if (!storedPlatformIds.Contains(platform.Id))
                {
                    continue;
                }

                foreach (var websiteId in platform.WebsiteIds.Distinct())
                {
                    if (
                        websitePlatforms.TryGetValue(websiteId, out var existingPlatformId)
                        && existingPlatformId != platform.Id
                    )
                    {
                        throw new InvalidDataException(
                            $"IGDB platform website {websiteId} belongs to both {existingPlatformId} and {platform.Id}."
                        );
                    }

                    websitePlatforms[websiteId] = platform.Id;
                }
            }

            platformCursor = nextCursor;
            if (platforms.Count < PageSize)
            {
                break;
            }
        }

        var websites = new List<IgdbPlatformWebsite>();
        var websiteCursor = -1L;
        while (true)
        {
            var page = await _igdbService.FetchPlatformWebsitesPageAsync(websiteCursor, cancellationToken);
            if (page.Count == 0)
            {
                break;
            }

            var nextCursor = page.Max(value => value.Id);
            ValidatePageCursor(websiteCursor, nextCursor, stage.Kind);
            websites.AddRange(page);
            websiteCursor = nextCursor;
            if (page.Count < PageSize)
            {
                break;
            }
        }

        var now = _dateTimeProvider.UtcNow;
        var links = websites
            .Where(value =>
                websitePlatforms.ContainsKey(value.Id)
                && storedWebsiteTypeIds.Contains(value.WebsiteTypeId)
                && !string.IsNullOrWhiteSpace(value.Url)
            )
            .DistinctBy(value => value.Id)
            .Select(value => new PlatformLink
            {
                Id = value.Id,
                PlatformId = websitePlatforms[value.Id],
                WebsiteTypeId = value.WebsiteTypeId,
                Url = value.Url!,
                Trusted = value.Trusted,
                Checksum = value.Checksum,
                CreatedAt = now,
                UpdatedAt = now,
                LastSyncedAt = now,
            })
            .ToList();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await _dbContext.PlatformLinks.ExecuteDeleteAsync(cancellationToken);
        if (links.Count > 0)
        {
            await _dbContext.BulkInsertAsync(links, cancellationToken: cancellationToken);
        }

        UpdatePageProgress(run, stage, websiteCursor, websites.Count, now);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await MarkStageSucceededAsync(run, stage, cancellationToken);
    }

    private async Task SyncGamesAsync(
        IgdbSyncRun run,
        IgdbSyncStage stage,
        IgdbSyncWindow? window,
        CancellationToken cancellationToken
    )
    {
        var cursor = stage.PageCursor;
        while (true)
        {
            var page = await _igdbService.FetchGamesPageAsync(cursor, window, cancellationToken);
            if (page.Count == 0)
            {
                break;
            }

            var nextCursor = page.Max(value => value.Id);
            ValidatePageCursor(cursor, nextCursor, stage.Kind);
            var now = _dateTimeProvider.UtcNow;
            var games = page.DistinctBy(value => value.Id).Select(value => MapGame(value, now)).ToList();
            var gameIds = games.Select(value => value.Id).ToArray();
            var gameGenres = page.SelectMany(value =>
                    value.GenreIds.Distinct().Select(id => new GameGenre { GameId = value.Id, GenreId = id })
                )
                .DistinctBy(value => (value.GameId, value.GenreId))
                .ToList();
            var gameThemes = page.SelectMany(value =>
                    value.ThemeIds.Distinct().Select(id => new GameTheme { GameId = value.Id, ThemeId = id })
                )
                .DistinctBy(value => (value.GameId, value.ThemeId))
                .ToList();
            var gameModes = page.SelectMany(value =>
                    value.GameModeIds.Distinct().Select(id => new GameGameMode { GameId = value.Id, GameModeId = id })
                )
                .DistinctBy(value => (value.GameId, value.GameModeId))
                .ToList();
            var gameEngines = page.SelectMany(value =>
                    value
                        .GameEngineIds.Distinct()
                        .Select(id => new GameGameEngine { GameId = value.Id, GameEngineId = id })
                )
                .DistinctBy(value => (value.GameId, value.GameEngineId))
                .ToList();
            var perspectives = page.SelectMany(value =>
                    value
                        .PlayerPerspectiveIds.Distinct()
                        .Select(id => new GamePlayerPerspective { GameId = value.Id, PlayerPerspectiveId = id })
                )
                .DistinctBy(value => (value.GameId, value.PlayerPerspectiveId))
                .ToList();
            var gamePlatforms = page.SelectMany(value =>
                    value.PlatformIds.Distinct().Select(id => new GamePlatform { GameId = value.Id, PlatformId = id })
                )
                .DistinctBy(value => (value.GameId, value.PlatformId))
                .ToList();
            var gameCollections = page.SelectMany(value =>
                    value
                        .CollectionIds.Distinct()
                        .Select(id => new GameCollection { GameId = value.Id, CollectionId = id })
                )
                .DistinctBy(value => (value.GameId, value.CollectionId))
                .ToList();
            var gameFranchises = page.SelectMany(value =>
                {
                    IEnumerable<long> franchiseIds = value.FranchiseIds;
                    if (value.FranchiseId is { } franchiseId)
                    {
                        franchiseIds = franchiseIds.Append(franchiseId);
                    }

                    return franchiseIds
                        .Distinct()
                        .Select(id => new GameFranchise { GameId = value.Id, FranchiseId = id });
                })
                .DistinctBy(value => (value.GameId, value.FranchiseId))
                .ToList();
            var touchedRelations = page.SelectMany(value =>
                    Enum.GetValues<GameRelationType>()
                        .Select(type => new IgdbSyncTouchedRelationParent
                        {
                            RunId = run.Id,
                            GameId = value.Id,
                            RelationType = type,
                        })
                )
                .ToList();
            var pendingRelations = page.SelectMany(value => CreatePendingRelations(run.Id, value))
                .DistinctBy(value => (value.RunId, value.GameId, value.RelatedGameId, value.RelationType))
                .ToList();

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await _dbContext.BulkInsertOrUpdateAsync(games, UpsertConfig, cancellationToken: cancellationToken);
            await DeleteGameJoinsAsync(gameIds, cancellationToken);
            await BulkInsertAsync(gameGenres, cancellationToken);
            await BulkInsertAsync(gameThemes, cancellationToken);
            await BulkInsertAsync(gameModes, cancellationToken);
            await BulkInsertAsync(gameEngines, cancellationToken);
            await BulkInsertAsync(perspectives, cancellationToken);
            await BulkInsertAsync(gamePlatforms, cancellationToken);
            await BulkInsertAsync(gameCollections, cancellationToken);
            await BulkInsertAsync(gameFranchises, cancellationToken);
            await BulkInsertAsync(touchedRelations, cancellationToken);
            await BulkInsertAsync(pendingRelations, cancellationToken);
            UpdatePageProgress(run, stage, nextCursor, page.Count, now);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            cursor = nextCursor;
            if (page.Count < PageSize)
            {
                break;
            }
        }

        await MarkStageSucceededAsync(run, stage, cancellationToken);
    }

    private async Task SyncGameRelationsAsync(IgdbSyncRun run, IgdbSyncStage stage, CancellationToken cancellationToken)
    {
        var cursor = stage.PageCursor;
        while (true)
        {
            var gameIds = await _dbContext
                .IgdbSyncTouchedRelationParents.AsNoTracking()
                .Where(value => value.RunId == run.Id && value.GameId > cursor)
                .Select(value => value.GameId)
                .Distinct()
                .OrderBy(value => value)
                .Take(PageSize)
                .ToArrayAsync(cancellationToken);
            if (gameIds.Length == 0)
            {
                break;
            }

            var pendingRelations = await _dbContext
                .IgdbSyncPendingGameRelations.AsNoTracking()
                .Where(value => value.RunId == run.Id && gameIds.Contains(value.GameId))
                .ToListAsync(cancellationToken);
            var nextCursor = gameIds[^1];
            var now = _dateTimeProvider.UtcNow;
            var preparedPage = await PrepareGameRelationsPageAsync(
                stage.Id,
                pendingRelations,
                now,
                cancellationToken
            );

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await _dbContext
                .GameRelations.Where(value => gameIds.Contains(value.GameId))
                .ExecuteDeleteAsync(cancellationToken);
            await BulkInsertAsync(preparedPage.Entities.ToList(), cancellationToken);
            if (preparedPage.SkippedRows.Count > 0)
            {
                await _dbContext.BulkInsertOrUpdateAsync(
                    preparedPage.SkippedRows,
                    SkippedRowUpsertConfig,
                    cancellationToken: cancellationToken
                );
            }

            UpdatePageProgress(run, stage, nextCursor, pendingRelations.Count, now);
            UpdateSkippedProgress(run, stage, preparedPage.SkippedEntityCount);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            LogSkippedRows(run.Id, stage.Kind, preparedPage);
            cursor = nextCursor;
        }

        await using (var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            await _dbContext
                .IgdbSyncPendingGameRelations.Where(value => value.RunId == run.Id)
                .ExecuteDeleteAsync(cancellationToken);
            await _dbContext
                .IgdbSyncTouchedRelationParents.Where(value => value.RunId == run.Id)
                .ExecuteDeleteAsync(cancellationToken);
            await MarkStageSucceededAsync(run, stage, cancellationToken, saveChanges: false);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
    }

    private async Task<ValidatedDependentPage<GameRelation>> PrepareGameRelationsPageAsync(
        Guid stageId,
        IReadOnlyList<IgdbSyncPendingGameRelation> pendingRelations,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var requestedRelatedGameIds = pendingRelations.Select(value => value.RelatedGameId).Distinct().ToArray();
        var storedRelatedGameIds = await _dbContext
            .Games.AsNoTracking()
            .Where(value => requestedRelatedGameIds.Contains(value.Id))
            .Select(value => value.Id)
            .ToHashSetAsync(cancellationToken);
        var relations = new List<GameRelation>(pendingRelations.Count);
        var skippedRows = new List<IgdbSyncSkippedRow>();
        var skippedRelationCount = 0;

        foreach (var pendingRelation in pendingRelations)
        {
            if (!storedRelatedGameIds.Contains(pendingRelation.RelatedGameId))
            {
                skippedRows.Add(
                    CreateSkippedRow(
                        stageId,
                        pendingRelation.GameId,
                        IgdbSyncSkipReason.MissingGame,
                        pendingRelation.RelatedGameId,
                        now
                    )
                );
                skippedRelationCount++;
                continue;
            }

            relations.Add(
                new GameRelation
                {
                    GameId = pendingRelation.GameId,
                    RelatedGameId = pendingRelation.RelatedGameId,
                    RelationType = pendingRelation.RelationType,
                    CreatedAt = now,
                }
            );
        }

        var distinctSkippedRows = skippedRows
            .DistinctBy(value => new { value.StageId, value.EntityId, value.Reason, value.MissingDependencyId })
            .ToList();
        return new ValidatedDependentPage<GameRelation>(relations, distinctSkippedRows, skippedRelationCount);
    }

    private async Task DeleteGameJoinsAsync(long[] gameIds, CancellationToken cancellationToken)
    {
        await _dbContext
            .GameGenres.Where(value => gameIds.Contains(value.GameId))
            .ExecuteDeleteAsync(cancellationToken);
        await _dbContext
            .GameThemes.Where(value => gameIds.Contains(value.GameId))
            .ExecuteDeleteAsync(cancellationToken);
        await _dbContext
            .GameGameModes.Where(value => gameIds.Contains(value.GameId))
            .ExecuteDeleteAsync(cancellationToken);
        await _dbContext
            .GameGameEngines.Where(value => gameIds.Contains(value.GameId))
            .ExecuteDeleteAsync(cancellationToken);
        await _dbContext
            .GamePlayerPerspectives.Where(value => gameIds.Contains(value.GameId))
            .ExecuteDeleteAsync(cancellationToken);
        await _dbContext
            .GamePlatforms.Where(value => gameIds.Contains(value.GameId))
            .ExecuteDeleteAsync(cancellationToken);
        await _dbContext
            .GameCollections.Where(value => gameIds.Contains(value.GameId))
            .ExecuteDeleteAsync(cancellationToken);
        await _dbContext
            .GameFranchises.Where(value => gameIds.Contains(value.GameId))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private Task BulkInsertAsync<TEntity>(IList<TEntity> entities, CancellationToken cancellationToken)
        where TEntity : class
    {
        if (entities.Count == 0)
        {
            return Task.CompletedTask;
        }

        return _dbContext.BulkInsertAsync(entities, cancellationToken: cancellationToken);
    }

    private async Task MarkStageSucceededAsync(
        IgdbSyncRun run,
        IgdbSyncStage stage,
        CancellationToken cancellationToken,
        bool saveChanges = true
    )
    {
        var now = _dateTimeProvider.UtcNow;
        stage.Status = IgdbSyncStageStatus.Succeeded;
        stage.CompletedAt = now;
        stage.UpdatedAt = now;
        stage.Error = null;
        run.UpdatedAt = now;
        if (saveChanges)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Completed IGDB stage {Stage} for run {RunId}: {Pages} pages, {Rows} rows, and {RowsSkipped} skipped rows.",
            stage.Kind,
            run.Id,
            stage.PagesProcessed,
            stage.RowsProcessed,
            stage.RowsSkipped
        );
    }

    private async Task RecordFailureAsync(
        Guid runId,
        IgdbSyncStageKind stageKind,
        int retryCount,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        _dbContext.ChangeTracker.Clear();
        var (run, stage) = await LoadStageAsync(runId, stageKind, cancellationToken);
        var now = _dateTimeProvider.UtcNow;
        stage.Status = IgdbSyncStageStatus.Failed;
        stage.Error = exception.Message;
        stage.UpdatedAt = now;
        run.Error = $"{stageKind}: {exception.Message}";
        run.UpdatedAt = now;
        if (retryCount >= FinalRetryCount)
        {
            run.Status = IgdbSyncRunStatus.Failed;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<(IgdbSyncRun Run, IgdbSyncStage Stage)> LoadStageAsync(
        Guid runId,
        IgdbSyncStageKind stageKind,
        CancellationToken cancellationToken
    )
    {
        var stage = await _dbContext.IgdbSyncStages.SingleAsync(
            value => value.RunId == runId && value.Kind == stageKind,
            cancellationToken
        );
        var run = await _dbContext.IgdbSyncRuns.SingleAsync(value => value.Id == runId, cancellationToken);
        return (run, stage);
    }

    private static Game MapGame(IgdbGame value, DateTime now)
    {
        decimal? totalRating = null;
        if (value.TotalRating.HasValue)
        {
            totalRating = Convert.ToDecimal(value.TotalRating.Value);
        }

        return new Game
        {
            Id = value.Id,
            Name = Required(value.Name, nameof(IgdbGame), value.Id),
            Slug = value.Slug,
            Summary = value.Summary,
            Storyline = value.Storyline,
            FirstReleaseDate = ToUtcDateTime(value.FirstReleaseDate),
            TotalRating = totalRating,
            TotalRatingCount = value.TotalRatingCount,
            IgdbUrl = value.Url,
            GameTypeId = value.GameTypeId,
            GameStatusId = value.GameStatusId,
            VersionParentId = value.VersionParentId,
            CoverImageId = value.Cover?.ImageId,
            CoverWidth = value.Cover?.Width,
            CoverHeight = value.Cover?.Height,
            CoverSmallUrl = value.Cover?.SmallUrl,
            CoverLargeUrl = value.Cover?.LargeUrl,
            Checksum = value.Checksum,
            IgdbUpdatedAt = ToUtcDateTime(value.UpdatedAt),
            CreatedAt = now,
            UpdatedAt = now,
            LastSyncedAt = now,
        };
    }

    private static IEnumerable<IgdbSyncPendingGameRelation> CreatePendingRelations(Guid runId, IgdbGame game)
    {
        return game
            .DlcIds.Select(id => new IgdbSyncPendingGameRelation
            {
                RunId = runId,
                GameId = game.Id,
                RelatedGameId = id,
                RelationType = GameRelationType.Dlc,
            })
            .Concat(
                game.ExpansionIds.Select(id => new IgdbSyncPendingGameRelation
                {
                    RunId = runId,
                    GameId = game.Id,
                    RelatedGameId = id,
                    RelationType = GameRelationType.Expansion,
                })
            )
            .Concat(
                game.StandaloneExpansionIds.Select(id => new IgdbSyncPendingGameRelation
                {
                    RunId = runId,
                    GameId = game.Id,
                    RelatedGameId = id,
                    RelationType = GameRelationType.StandaloneExpansion,
                })
            )
            .Where(value => value.GameId != value.RelatedGameId);
    }

    private static IgdbSyncWindow CreateWindow(IgdbSyncRun run)
    {
        DateTimeOffset? from = null;
        if (run.From.HasValue)
        {
            from = ToUtcOffset(run.From.Value);
        }

        return new IgdbSyncWindow(from, ToUtcOffset(run.Through));
    }

    private static DateTimeOffset ToUtcOffset(DateTime value)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    private static DateTime? ToUtcDateTime(DateTimeOffset? value)
    {
        return value?.UtcDateTime;
    }

    private static string Required(string? value, string resourceName, long id)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"{resourceName} {id} has a missing required name.");
        }

        return value;
    }

    private static void ValidatePageCursor(long currentCursor, long nextCursor, IgdbSyncStageKind stageKind)
    {
        if (nextCursor <= currentCursor)
        {
            throw new InvalidDataException(
                $"IGDB stage {stageKind} did not advance its page cursor beyond {currentCursor}."
            );
        }
    }

    private static void UpdatePageProgress(
        IgdbSyncRun run,
        IgdbSyncStage stage,
        long cursor,
        int rowsProcessed,
        DateTime now
    )
    {
        stage.PageCursor = cursor;
        stage.PagesProcessed++;
        stage.RowsProcessed += rowsProcessed;
        stage.UpdatedAt = now;
        run.RowsProcessed += rowsProcessed;
        run.UpdatedAt = now;
    }

    private static void UpdateSkippedProgress(IgdbSyncRun run, IgdbSyncStage stage, int rowsSkipped)
    {
        stage.RowsSkipped += rowsSkipped;
        run.RowsSkipped += rowsSkipped;
    }

    private sealed record ValidatedDependentPage<TEntity>(
        IReadOnlyList<TEntity> Entities,
        IReadOnlyList<IgdbSyncSkippedRow> SkippedRows,
        int SkippedEntityCount
    );
}
