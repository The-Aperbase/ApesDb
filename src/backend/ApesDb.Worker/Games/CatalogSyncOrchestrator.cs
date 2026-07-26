using ApesDb.Common;
using ApesDb.Domain;
using ApesDb.Domain.Entities.IgdbSync;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TickerQ.Utilities;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Enums;
using TickerQ.Utilities.Interfaces.Managers;

namespace ApesDb.Worker.Games;

public sealed class CatalogSyncOrchestrator : ICatalogSyncOrchestrator
{
    private const string UnfinishedRunIndex = "UX_IgdbSyncRuns_Unfinished";

    private static readonly TimeSpan CatalogConsistencyLag = TimeSpan.FromMinutes(5);

    private static readonly IgdbSyncStageKind[] BootstrapStages =
    [
        IgdbSyncStageKind.GameTypes,
        IgdbSyncStageKind.GameStatuses,
        IgdbSyncStageKind.Genres,
        IgdbSyncStageKind.Themes,
        IgdbSyncStageKind.GameModes,
        IgdbSyncStageKind.GameEngines,
        IgdbSyncStageKind.PlayerPerspectives,
        IgdbSyncStageKind.PlatformTypes,
        IgdbSyncStageKind.WebsiteTypes,
        IgdbSyncStageKind.PopularityTypes,
        IgdbSyncStageKind.ExternalGameSources,
        IgdbSyncStageKind.Companies,
        IgdbSyncStageKind.Collections,
        IgdbSyncStageKind.Franchises,
        IgdbSyncStageKind.Platforms,
        IgdbSyncStageKind.PlatformLinks,
        IgdbSyncStageKind.Games,
        IgdbSyncStageKind.GameRelations,
        IgdbSyncStageKind.InvolvedCompanies,
        IgdbSyncStageKind.ExternalGames,
        IgdbSyncStageKind.Popularity,
        IgdbSyncStageKind.Complete,
    ];

    private static readonly IgdbSyncStageKind[] IncrementalStages =
    [
        IgdbSyncStageKind.GameTypes,
        IgdbSyncStageKind.GameStatuses,
        IgdbSyncStageKind.Genres,
        IgdbSyncStageKind.Themes,
        IgdbSyncStageKind.GameModes,
        IgdbSyncStageKind.GameEngines,
        IgdbSyncStageKind.PlayerPerspectives,
        IgdbSyncStageKind.PlatformTypes,
        IgdbSyncStageKind.WebsiteTypes,
        IgdbSyncStageKind.PopularityTypes,
        IgdbSyncStageKind.ExternalGameSources,
        IgdbSyncStageKind.Companies,
        IgdbSyncStageKind.Collections,
        IgdbSyncStageKind.Franchises,
        IgdbSyncStageKind.Platforms,
        IgdbSyncStageKind.PlatformLinks,
        IgdbSyncStageKind.Games,
        IgdbSyncStageKind.GameRelations,
        IgdbSyncStageKind.InvolvedCompanies,
        IgdbSyncStageKind.ExternalGames,
        IgdbSyncStageKind.Complete,
    ];

    private readonly ApplicationDbContext _dbContext;
    private readonly IDbContextFactory<WorkerTickerQDbContext> _tickerDbContextFactory;
    private readonly ITimeTickerManager<TimeTickerEntity> _timeTickerManager;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CatalogSyncOrchestrator> _logger;

    public CatalogSyncOrchestrator(
        ApplicationDbContext dbContext,
        IDbContextFactory<WorkerTickerQDbContext> tickerDbContextFactory,
        ITimeTickerManager<TimeTickerEntity> timeTickerManager,
        IDateTimeProvider dateTimeProvider,
        ILogger<CatalogSyncOrchestrator> logger
    )
    {
        _dbContext = dbContext;
        _tickerDbContextFactory = tickerDbContextFactory;
        _timeTickerManager = timeTickerManager;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task EnsureBootstrapAsync(CancellationToken cancellationToken = default)
    {
        var unfinished = await FindUnfinishedRunAsync(cancellationToken);
        if (unfinished is not null)
        {
            if (unfinished.Status == IgdbSyncRunStatus.Failed)
            {
                LogFailedRunRequiresResume(unfinished);
                return;
            }

            await EnsureRunScheduledAsync(unfinished, cancellationToken);
            return;
        }

        if (
            await _dbContext.IgdbSyncRuns.AnyAsync(
                run => run.Mode == IgdbSyncRunMode.Bootstrap && run.Status == IgdbSyncRunStatus.Succeeded,
                cancellationToken
            )
        )
        {
            await EnsureIncrementalAsync(cancellationToken);
            return;
        }

        var now = CaptureRunThrough();
        await CreateAndScheduleRunAsync(
            IgdbSyncRunMode.Bootstrap,
            from: null,
            through: now,
            BootstrapStages,
            cancellationToken
        );
    }

    public async Task StartFullSyncAsync(CancellationToken cancellationToken = default)
    {
        var unfinished = await FindUnfinishedRunAsync(cancellationToken);
        if (unfinished is not null)
        {
            throw CreateFullSyncConflictException(unfinished);
        }

        var now = CaptureRunThrough();
        await CreateAndScheduleRunAsync(
            IgdbSyncRunMode.Bootstrap,
            from: null,
            through: now,
            BootstrapStages,
            cancellationToken,
            failOnUnfinishedRunConflict: true
        );
    }

    public async Task ResumeFailedAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await _dbContext.IgdbSyncRuns.SingleOrDefaultAsync(value => value.Id == runId, cancellationToken);
        if (run is null)
        {
            throw new InvalidOperationException($"IGDB run {runId} does not exist.");
        }

        if (run.Status != IgdbSyncRunStatus.Failed)
        {
            throw new InvalidOperationException(
                $"IGDB run {run.Id} cannot be resumed because its status is {run.Status}, not Failed."
            );
        }

        var stage = await _dbContext
            .IgdbSyncStages.Where(value => value.RunId == run.Id && value.Status != IgdbSyncStageStatus.Succeeded)
            .OrderBy(value => value.Order)
            .FirstOrDefaultAsync(cancellationToken);
        if (stage is null)
        {
            throw new InvalidDataException($"Failed IGDB run {run.Id} has no incomplete stage to resume.");
        }

        await ScheduleStageAsync(run, stage, cancellationToken, requireFailedRun: true);
        _logger.LogInformation(
            "Resumed failed IGDB run {RunId} at stage {Stage} from cursor {Cursor}.",
            run.Id,
            stage.Kind,
            stage.PageCursor
        );
    }

    public async Task EnsureIncrementalAsync(CancellationToken cancellationToken = default)
    {
        var unfinished = await FindUnfinishedRunAsync(cancellationToken);
        if (unfinished is not null)
        {
            if (unfinished.Status == IgdbSyncRunStatus.Failed)
            {
                LogFailedRunRequiresResume(unfinished);
                return;
            }

            await EnsureRunScheduledAsync(unfinished, cancellationToken);
            return;
        }

        var previous = await _dbContext
            .IgdbSyncRuns.AsNoTracking()
            .Where(run => run.Status == IgdbSyncRunStatus.Succeeded)
            .OrderByDescending(run => run.Through)
            .FirstOrDefaultAsync(cancellationToken);
        if (previous is null)
        {
            await EnsureBootstrapAsync(cancellationToken);
            return;
        }

        var now = CaptureRunThrough();
        if (now <= previous.Through)
        {
            return;
        }

        await CreateAndScheduleRunAsync(
            IgdbSyncRunMode.Incremental,
            previous.Through,
            now,
            IncrementalStages,
            cancellationToken
        );
    }

    public async Task CompleteAsync(Guid runId, int retryCount, CancellationToken cancellationToken = default)
    {
        try
        {
            await CompleteCoreAsync(runId, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await RecordCompletionFailureAsync(runId, retryCount, exception, CancellationToken.None);
            throw;
        }
    }

    private async Task CompleteCoreAsync(Guid runId, CancellationToken cancellationToken)
    {
        var run = await _dbContext.IgdbSyncRuns.SingleAsync(value => value.Id == runId, cancellationToken);
        if (run.Status == IgdbSyncRunStatus.Succeeded)
        {
            if (run.Mode == IgdbSyncRunMode.Bootstrap)
            {
                await EnsureIncrementalAsync(cancellationToken);
            }

            return;
        }

        var completionStage = await _dbContext.IgdbSyncStages.SingleAsync(
            value => value.RunId == runId && value.Kind == IgdbSyncStageKind.Complete,
            cancellationToken
        );
        var startedAt = _dateTimeProvider.UtcNow;
        run.Status = IgdbSyncRunStatus.Running;
        run.StartedAt ??= startedAt;
        run.UpdatedAt = startedAt;
        run.Error = null;
        completionStage.Status = IgdbSyncStageStatus.Running;
        completionStage.StartedAt ??= startedAt;
        completionStage.UpdatedAt = startedAt;
        completionStage.Error = null;
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (
            await _dbContext.IgdbSyncStages.AnyAsync(
                value =>
                    value.RunId == runId
                    && value.Kind != IgdbSyncStageKind.Complete
                    && value.Status != IgdbSyncStageStatus.Succeeded,
                cancellationToken
            )
        )
        {
            throw new InvalidOperationException(
                $"IGDB run {runId} cannot complete while an earlier stage is unfinished."
            );
        }

        var now = _dateTimeProvider.UtcNow;
        run.Status = IgdbSyncRunStatus.Succeeded;
        run.CompletedAt = now;
        run.UpdatedAt = now;
        run.Error = null;

        completionStage.Status = IgdbSyncStageStatus.Succeeded;
        completionStage.StartedAt ??= now;
        completionStage.CompletedAt = now;
        completionStage.UpdatedAt = now;
        completionStage.Error = null;

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Completed {Mode} IGDB catalog run {RunId} through {Through} with {RowsProcessed} processed rows and "
                + "{RowsSkipped} skipped rows.",
            run.Mode,
            run.Id,
            run.Through,
            run.RowsProcessed,
            run.RowsSkipped
        );

        if (run.Mode == IgdbSyncRunMode.Bootstrap)
        {
            await EnsureIncrementalAsync(cancellationToken);
        }
    }

    public async Task AdvanceAsync(
        Guid runId,
        IgdbSyncStageKind completedStage,
        CancellationToken cancellationToken = default
    )
    {
        var current = await _dbContext.IgdbSyncStages.SingleAsync(
            value => value.RunId == runId && value.Kind == completedStage,
            cancellationToken
        );
        if (current.Status != IgdbSyncStageStatus.Succeeded)
        {
            throw new InvalidOperationException(
                $"IGDB run {runId} cannot advance because stage {completedStage} has not succeeded."
            );
        }

        var next = await _dbContext
            .IgdbSyncStages.AsNoTracking()
            .Where(value => value.RunId == runId && value.Order > current.Order)
            .OrderBy(value => value.Order)
            .FirstOrDefaultAsync(cancellationToken);
        if (next is not null)
        {
            await ScheduleStageAsync(
                await _dbContext.IgdbSyncRuns.SingleAsync(value => value.Id == runId, cancellationToken),
                next,
                cancellationToken
            );
        }
    }

    private Task<IgdbSyncRun?> FindUnfinishedRunAsync(CancellationToken cancellationToken)
    {
        return _dbContext.IgdbSyncRuns.SingleOrDefaultAsync(
            run => run.Status != IgdbSyncRunStatus.Succeeded,
            cancellationToken
        );
    }

    private async Task CreateAndScheduleRunAsync(
        IgdbSyncRunMode mode,
        DateTime? from,
        DateTime through,
        IReadOnlyList<IgdbSyncStageKind> stageKinds,
        CancellationToken cancellationToken,
        bool failOnUnfinishedRunConflict = false
    )
    {
        var now = _dateTimeProvider.UtcNow;
        var run = new IgdbSyncRun
        {
            Id = Guid.CreateVersion7(),
            Mode = mode,
            Status = IgdbSyncRunStatus.Pending,
            From = from,
            Through = through,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var stages = stageKinds
            .Select(
                (kind, index) =>
                    new IgdbSyncStage
                    {
                        Id = Guid.CreateVersion7(),
                        RunId = run.Id,
                        Kind = kind,
                        Order = index + 1,
                        Status = IgdbSyncStageStatus.Pending,
                        CreatedAt = now,
                        UpdatedAt = now,
                    }
            )
            .ToArray();

        _dbContext.IgdbSyncRuns.Add(run);
        _dbContext.IgdbSyncStages.AddRange(stages);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUnfinishedRunConflict(exception))
        {
            _dbContext.ChangeTracker.Clear();
            var winner = await FindUnfinishedRunAsync(cancellationToken);
            if (winner is null)
            {
                throw;
            }

            if (failOnUnfinishedRunConflict)
            {
                throw CreateFullSyncConflictException(winner);
            }

            await EnsureRunScheduledAsync(winner, cancellationToken);
            return;
        }

        await ScheduleStageAsync(run, stages[0], cancellationToken);
    }

    private async Task EnsureRunScheduledAsync(IgdbSyncRun run, CancellationToken cancellationToken)
    {
        while (true)
        {
            var stage = await _dbContext
                .IgdbSyncStages.Where(stage => stage.RunId == run.Id && stage.Status != IgdbSyncStageStatus.Succeeded)
                .OrderBy(stage => stage.Order)
                .FirstOrDefaultAsync(cancellationToken);
            if (stage is null)
            {
                await _dbContext.Entry(run).ReloadAsync(cancellationToken);
                if (run.Status == IgdbSyncRunStatus.Succeeded)
                {
                    return;
                }

                throw new InvalidDataException($"Unfinished IGDB run {run.Id} has no incomplete stage to resume.");
            }

            await ScheduleStageAsync(run, stage, cancellationToken);
            await _dbContext.Entry(stage).ReloadAsync(cancellationToken);
            if (stage.Status == IgdbSyncStageStatus.Succeeded)
            {
                continue;
            }

            _logger.LogInformation(
                "Ensured stage {Stage} is scheduled for IGDB run {RunId} from cursor {Cursor}.",
                stage.Kind,
                run.Id,
                stage.PageCursor
            );
            return;
        }
    }

    private async Task ScheduleStageAsync(
        IgdbSyncRun run,
        IgdbSyncStage stage,
        CancellationToken cancellationToken,
        bool requireFailedRun = false
    )
    {
        var connectionString = _dbContext.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("The catalog database connection string is unavailable.");
        }

        await using var lockConnection = new NpgsqlConnection(connectionString);
        await lockConnection.OpenAsync(cancellationToken);
        await SetStageSchedulingLockAsync(lockConnection, stage.Id, acquire: true, cancellationToken);
        try
        {
            await ScheduleStageCoreAsync(run, stage, cancellationToken, requireFailedRun);
        }
        finally
        {
            await SetStageSchedulingLockAsync(lockConnection, stage.Id, acquire: false, CancellationToken.None);
        }
    }

    private async Task ScheduleStageCoreAsync(
        IgdbSyncRun run,
        IgdbSyncStage stage,
        CancellationToken cancellationToken,
        bool requireFailedRun
    )
    {
        await _dbContext.Entry(run).ReloadAsync(cancellationToken);
        if (_dbContext.Entry(stage).State == EntityState.Detached)
        {
            _dbContext.Attach(stage);
        }

        await _dbContext.Entry(stage).ReloadAsync(cancellationToken);
        if (requireFailedRun && run.Status != IgdbSyncRunStatus.Failed)
        {
            throw new InvalidOperationException(
                $"IGDB run {run.Id} changed to {run.Status} before the failed stage could be resumed."
            );
        }

        if (run.Status == IgdbSyncRunStatus.Succeeded || stage.Status == IgdbSyncStageStatus.Succeeded)
        {
            return;
        }

        var existingTicker = await FindTickerAsync(stage.Id, cancellationToken);
        if (existingTicker is not null)
        {
            if (IsActive(existingTicker.Status))
            {
                if (requireFailedRun)
                {
                    throw new InvalidOperationException(
                        $"IGDB run {run.Id} cannot be resumed while ticker {existingTicker.Id} is "
                            + $"{existingTicker.Status} on node {existingTicker.LockHolder ?? "unknown"}."
                    );
                }

                return;
            }

            var deletion = await _timeTickerManager.DeleteAsync(existingTicker.Id, cancellationToken);
            if (!deletion.IsSucceeded)
            {
                var currentTicker = await FindTickerAsync(existingTicker.Id, cancellationToken);
                if (currentTicker is not null && IsActive(currentTicker.Status))
                {
                    return;
                }

                if (currentTicker is not null)
                {
                    throw new InvalidOperationException(
                        $"Could not remove terminal ticker {existingTicker.Id} for IGDB run {run.Id}.",
                        deletion.Exception
                    );
                }
            }
        }

        var now = _dateTimeProvider.UtcNow;
        run.Status = IgdbSyncRunStatus.Pending;
        run.Error = null;
        run.UpdatedAt = now;
        stage.Status = IgdbSyncStageStatus.Pending;
        stage.Error = null;
        stage.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var retryIntervals = CatalogSyncRetryPolicy.CreateIntervals();
        var ticker = new TimeTickerEntity
        {
            Id = stage.Id,
            Function = CatalogSyncFunctions.ForStage(stage.Kind),
            Description = $"IGDB {run.Mode} run {run.Id}: {stage.Kind}",
            Request = TickerHelper.CreateTickerRequest(new CatalogSyncJobRequest(run.Id)),
            Retries = CatalogSyncRetryPolicy.MaxRetries,
            RetryIntervals = retryIntervals,
            ExecutionTime = now,
        };

        var result = await _timeTickerManager.AddAsync(ticker, cancellationToken);
        if (!result.IsSucceeded)
        {
            var concurrentlyScheduledTicker = await FindTickerAsync(ticker.Id, cancellationToken);
            if (concurrentlyScheduledTicker is not null && IsActive(concurrentlyScheduledTicker.Status))
            {
                return;
            }

            await _dbContext.Entry(run).ReloadAsync(cancellationToken);
            await _dbContext.Entry(stage).ReloadAsync(cancellationToken);
            if (run.Status == IgdbSyncRunStatus.Succeeded || stage.Status == IgdbSyncStageStatus.Succeeded)
            {
                return;
            }

            var error = result.Exception?.Message ?? "TickerQ did not schedule the IGDB sync stage.";
            await _dbContext
                .IgdbSyncRuns.Where(value => value.Id == run.Id && value.Status != IgdbSyncRunStatus.Succeeded)
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(value => value.Status, IgdbSyncRunStatus.Failed)
                            .SetProperty(value => value.Error, error)
                            .SetProperty(value => value.UpdatedAt, _dateTimeProvider.UtcNow),
                    cancellationToken
                );
            throw new InvalidOperationException("Could not schedule the IGDB catalog sync chain.", result.Exception);
        }

        _logger.LogInformation(
            "Scheduled stage {Stage} for {Mode} IGDB catalog run {RunId} ({From}, {Through}].",
            stage.Kind,
            run.Mode,
            run.Id,
            run.From,
            run.Through
        );
    }

    private static async Task SetStageSchedulingLockAsync(
        NpgsqlConnection connection,
        Guid stageId,
        bool acquire,
        CancellationToken cancellationToken
    )
    {
        var function = "pg_advisory_unlock";
        if (acquire)
        {
            function = "pg_advisory_lock";
        }

        await using var command = new NpgsqlCommand($"SELECT {function}(hashtextextended(@stage_id, 0));", connection);
        command.Parameters.AddWithValue("stage_id", stageId.ToString("D"));
        await command.ExecuteScalarAsync(cancellationToken);
    }

    private async Task RecordCompletionFailureAsync(
        Guid runId,
        int retryCount,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        _dbContext.ChangeTracker.Clear();
        var run = await _dbContext.IgdbSyncRuns.SingleAsync(value => value.Id == runId, cancellationToken);
        if (run.Status == IgdbSyncRunStatus.Succeeded)
        {
            return;
        }

        var stage = await _dbContext.IgdbSyncStages.SingleAsync(
            value => value.RunId == runId && value.Kind == IgdbSyncStageKind.Complete,
            cancellationToken
        );
        var now = _dateTimeProvider.UtcNow;
        stage.Status = IgdbSyncStageStatus.Failed;
        stage.Error = exception.Message;
        stage.UpdatedAt = now;
        run.Error = $"{IgdbSyncStageKind.Complete}: {exception.Message}";
        run.UpdatedAt = now;
        if (retryCount >= CatalogSyncRetryPolicy.MaxRetries)
        {
            run.Status = IgdbSyncRunStatus.Failed;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<TimeTickerEntity?> FindTickerAsync(Guid tickerId, CancellationToken cancellationToken)
    {
        await using var tickerDbContext = await _tickerDbContextFactory.CreateDbContextAsync(cancellationToken);
        return await tickerDbContext
            .Set<TimeTickerEntity>()
            .AsNoTracking()
            .SingleOrDefaultAsync(value => value.Id == tickerId, cancellationToken);
    }

    private DateTime CaptureRunThrough()
    {
        var laggedNow = _dateTimeProvider.UtcNow - CatalogConsistencyLag;
        return new DateTime(laggedNow.Ticks - (laggedNow.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
    }

    private static bool IsActive(TickerStatus status)
    {
        return status is TickerStatus.Idle or TickerStatus.Queued or TickerStatus.InProgress;
    }

    private static bool IsUnfinishedRunConflict(DbUpdateException exception)
    {
        return exception.InnerException
            is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: UnfinishedRunIndex };
    }

    private static InvalidOperationException CreateFullSyncConflictException(IgdbSyncRun run)
    {
        return new InvalidOperationException(
            $"Cannot start a full IGDB sync while run {run.Id} ({run.Mode}, {run.Status}) is unfinished."
        );
    }

    private void LogFailedRunRequiresResume(IgdbSyncRun run)
    {
        _logger.LogWarning(
            "IGDB run {RunId} is failed and will remain stopped until {ResumeFunction} is invoked.",
            run.Id,
            CatalogSyncFunctions.Resume
        );
    }
}
