namespace ApesDb.Worker.Games;

public interface ICatalogSyncOrchestrator
{
    Task EnsureBootstrapAsync(CancellationToken cancellationToken = default);

    Task StartFullSyncAsync(CancellationToken cancellationToken = default);

    Task ResumeFailedAsync(Guid runId, CancellationToken cancellationToken = default);

    Task EnsureIncrementalAsync(CancellationToken cancellationToken = default);

    Task AdvanceAsync(
        Guid runId,
        ApesDb.Domain.Entities.IgdbSync.IgdbSyncStageKind completedStage,
        CancellationToken cancellationToken = default
    );

    Task CompleteAsync(Guid runId, int retryCount, CancellationToken cancellationToken = default);
}
