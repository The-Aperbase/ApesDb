using ApesDb.Domain.Entities.IgdbSync;
using TickerQ.Utilities.Base;

namespace ApesDb.Worker.Games;

public sealed class CatalogSyncJobs
{
    private readonly ICatalogStageRunner _stageRunner;
    private readonly ICatalogSyncOrchestrator _orchestrator;
    private readonly IPopularitySynchronizer _popularitySynchronizer;

    public CatalogSyncJobs(
        ICatalogStageRunner stageRunner,
        ICatalogSyncOrchestrator orchestrator,
        IPopularitySynchronizer popularitySynchronizer
    )
    {
        _stageRunner = stageRunner;
        _orchestrator = orchestrator;
        _popularitySynchronizer = popularitySynchronizer;
    }

    [TickerFunction(CatalogSyncFunctions.ScheduleDaily, cronExpression: "0 2 * * *", maxConcurrency: 1)]
    public Task ScheduleDailyAsync(TickerFunctionContext context, CancellationToken cancellationToken)
    {
        context.CronOccurrenceOperations?.SkipIfAlreadyRunning();
        return _orchestrator.EnsureIncrementalAsync(cancellationToken);
    }

    [TickerFunction(CatalogSyncFunctions.StartFull, maxConcurrency: 1)]
    public Task StartFullAsync(TickerFunctionContext context, CancellationToken cancellationToken)
    {
        return _orchestrator.StartFullSyncAsync(cancellationToken);
    }

    [TickerFunction(CatalogSyncFunctions.Resume, maxConcurrency: 1)]
    public Task ResumeFailedAsync(
        TickerFunctionContext<CatalogSyncResumeRequest> context,
        CancellationToken cancellationToken
    )
    {
        return _orchestrator.ResumeFailedAsync(context.Request.RunId, cancellationToken);
    }

    [TickerFunction(CatalogSyncFunctions.GameTypes, maxConcurrency: 1)]
    public Task SyncGameTypesAsync(
        TickerFunctionContext<CatalogSyncJobRequest> context,
        CancellationToken cancellationToken
    ) => RunAsync(context, IgdbSyncStageKind.GameTypes, cancellationToken);

    [TickerFunction(CatalogSyncFunctions.GameStatuses, maxConcurrency: 1)]
    public Task SyncGameStatusesAsync(
        TickerFunctionContext<CatalogSyncJobRequest> context,
        CancellationToken cancellationToken
    ) => RunAsync(context, IgdbSyncStageKind.GameStatuses, cancellationToken);

    [TickerFunction(CatalogSyncFunctions.Genres, maxConcurrency: 1)]
    public Task SyncGenresAsync(
        TickerFunctionContext<CatalogSyncJobRequest> context,
        CancellationToken cancellationToken
    ) => RunAsync(context, IgdbSyncStageKind.Genres, cancellationToken);

    [TickerFunction(CatalogSyncFunctions.Themes, maxConcurrency: 1)]
    public Task SyncThemesAsync(
        TickerFunctionContext<CatalogSyncJobRequest> context,
        CancellationToken cancellationToken
    ) => RunAsync(context, IgdbSyncStageKind.Themes, cancellationToken);

    [TickerFunction(CatalogSyncFunctions.GameModes, maxConcurrency: 1)]
    public Task SyncGameModesAsync(
        TickerFunctionContext<CatalogSyncJobRequest> context,
        CancellationToken cancellationToken
    ) => RunAsync(context, IgdbSyncStageKind.GameModes, cancellationToken);

    [TickerFunction(CatalogSyncFunctions.GameEngines, maxConcurrency: 1)]
    public Task SyncGameEnginesAsync(
        TickerFunctionContext<CatalogSyncJobRequest> context,
        CancellationToken cancellationToken
    ) => RunAsync(context, IgdbSyncStageKind.GameEngines, cancellationToken);

    [TickerFunction(CatalogSyncFunctions.PlayerPerspectives, maxConcurrency: 1)]
    public Task SyncPlayerPerspectivesAsync(
        TickerFunctionContext<CatalogSyncJobRequest> context,
        CancellationToken cancellationToken
    ) => RunAsync(context, IgdbSyncStageKind.PlayerPerspectives, cancellationToken);

    [TickerFunction(CatalogSyncFunctions.PlatformTypes, maxConcurrency: 1)]
    public Task SyncPlatformTypesAsync(
        TickerFunctionContext<CatalogSyncJobRequest> context,
        CancellationToken cancellationToken
    ) => RunAsync(context, IgdbSyncStageKind.PlatformTypes, cancellationToken);

    [TickerFunction(CatalogSyncFunctions.WebsiteTypes, maxConcurrency: 1)]
    public Task SyncWebsiteTypesAsync(
        TickerFunctionContext<CatalogSyncJobRequest> context,
        CancellationToken cancellationToken
    ) => RunAsync(context, IgdbSyncStageKind.WebsiteTypes, cancellationToken);

    [TickerFunction(CatalogSyncFunctions.PopularityTypes, maxConcurrency: 1)]
    public Task SyncPopularityTypesAsync(
        TickerFunctionContext<CatalogSyncJobRequest> context,
        CancellationToken cancellationToken
    ) => RunAsync(context, IgdbSyncStageKind.PopularityTypes, cancellationToken);

    [TickerFunction(CatalogSyncFunctions.ExternalGameSources, maxConcurrency: 1)]
    public Task SyncExternalGameSourcesAsync(
        TickerFunctionContext<CatalogSyncJobRequest> context,
        CancellationToken cancellationToken
    ) => RunAsync(context, IgdbSyncStageKind.ExternalGameSources, cancellationToken);

    [TickerFunction(CatalogSyncFunctions.Companies, maxConcurrency: 1)]
    public Task SyncCompaniesAsync(
        TickerFunctionContext<CatalogSyncJobRequest> context,
        CancellationToken cancellationToken
    ) => RunAsync(context, IgdbSyncStageKind.Companies, cancellationToken);

    [TickerFunction(CatalogSyncFunctions.Collections, maxConcurrency: 1)]
    public Task SyncCollectionsAsync(
        TickerFunctionContext<CatalogSyncJobRequest> context,
        CancellationToken cancellationToken
    ) => RunAsync(context, IgdbSyncStageKind.Collections, cancellationToken);

    [TickerFunction(CatalogSyncFunctions.Franchises, maxConcurrency: 1)]
    public Task SyncFranchisesAsync(
        TickerFunctionContext<CatalogSyncJobRequest> context,
        CancellationToken cancellationToken
    ) => RunAsync(context, IgdbSyncStageKind.Franchises, cancellationToken);

    [TickerFunction(CatalogSyncFunctions.Platforms, maxConcurrency: 1)]
    public Task SyncPlatformsAsync(
        TickerFunctionContext<CatalogSyncJobRequest> context,
        CancellationToken cancellationToken
    ) => RunAsync(context, IgdbSyncStageKind.Platforms, cancellationToken);

    [TickerFunction(CatalogSyncFunctions.PlatformLinks, maxConcurrency: 1)]
    public Task SyncPlatformLinksAsync(
        TickerFunctionContext<CatalogSyncJobRequest> context,
        CancellationToken cancellationToken
    ) => RunAsync(context, IgdbSyncStageKind.PlatformLinks, cancellationToken);

    [TickerFunction(CatalogSyncFunctions.Games, maxConcurrency: 1)]
    public Task SyncGamesAsync(
        TickerFunctionContext<CatalogSyncJobRequest> context,
        CancellationToken cancellationToken
    ) => RunAsync(context, IgdbSyncStageKind.Games, cancellationToken);

    [TickerFunction(CatalogSyncFunctions.GameRelations, maxConcurrency: 1)]
    public Task SyncGameRelationsAsync(
        TickerFunctionContext<CatalogSyncJobRequest> context,
        CancellationToken cancellationToken
    ) => RunAsync(context, IgdbSyncStageKind.GameRelations, cancellationToken);

    [TickerFunction(CatalogSyncFunctions.InvolvedCompanies, maxConcurrency: 1)]
    public Task SyncInvolvedCompaniesAsync(
        TickerFunctionContext<CatalogSyncJobRequest> context,
        CancellationToken cancellationToken
    ) => RunAsync(context, IgdbSyncStageKind.InvolvedCompanies, cancellationToken);

    [TickerFunction(CatalogSyncFunctions.ExternalGames, maxConcurrency: 1)]
    public Task SyncExternalGamesAsync(
        TickerFunctionContext<CatalogSyncJobRequest> context,
        CancellationToken cancellationToken
    ) => RunAsync(context, IgdbSyncStageKind.ExternalGames, cancellationToken);

    [TickerFunction(CatalogSyncFunctions.Popularity, maxConcurrency: 1)]
    public Task SyncInitialPopularityAsync(
        TickerFunctionContext<CatalogSyncJobRequest> context,
        CancellationToken cancellationToken
    ) => RunAsync(context, IgdbSyncStageKind.Popularity, cancellationToken);

    [TickerFunction(CatalogSyncFunctions.Complete, maxConcurrency: 1)]
    public Task CompleteAsync(
        TickerFunctionContext<CatalogSyncJobRequest> context,
        CancellationToken cancellationToken
    ) => _orchestrator.CompleteAsync(context.Request.RunId, context.RetryCount, cancellationToken);

    [TickerFunction(CatalogSyncFunctions.RefreshPopularity, cronExpression: "0 * * * *", maxConcurrency: 1)]
    public async Task RefreshPopularityAsync(TickerFunctionContext context, CancellationToken cancellationToken)
    {
        context.CronOccurrenceOperations?.SkipIfAlreadyRunning();
        await _popularitySynchronizer.RefreshAsync(allowDuringCatalogRun: false, cancellationToken);
    }

    private async Task RunAsync(
        TickerFunctionContext<CatalogSyncJobRequest> context,
        IgdbSyncStageKind stageKind,
        CancellationToken cancellationToken
    )
    {
        await _stageRunner.RunAsync(context.Request.RunId, stageKind, context.RetryCount, cancellationToken);
        await _orchestrator.AdvanceAsync(context.Request.RunId, stageKind, cancellationToken);
    }
}
