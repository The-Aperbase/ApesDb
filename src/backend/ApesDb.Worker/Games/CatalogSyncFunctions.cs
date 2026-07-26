using ApesDb.Domain.Entities.IgdbSync;

namespace ApesDb.Worker.Games;

internal static class CatalogSyncFunctions
{
    public const string ScheduleDaily = "schedule-daily-igdb-sync";
    public const string StartFull = "start-full-igdb-sync";
    public const string Resume = "resume-igdb-sync";
    public const string GameTypes = "sync-igdb-game-types";
    public const string GameStatuses = "sync-igdb-game-statuses";
    public const string Genres = "sync-igdb-genres";
    public const string Themes = "sync-igdb-themes";
    public const string GameModes = "sync-igdb-game-modes";
    public const string GameEngines = "sync-igdb-game-engines";
    public const string PlayerPerspectives = "sync-igdb-player-perspectives";
    public const string PlatformTypes = "sync-igdb-platform-types";
    public const string WebsiteTypes = "sync-igdb-website-types";
    public const string PopularityTypes = "sync-igdb-popularity-types";
    public const string ExternalGameSources = "sync-igdb-external-game-sources";
    public const string Companies = "sync-igdb-companies";
    public const string Collections = "sync-igdb-collections";
    public const string Franchises = "sync-igdb-franchises";
    public const string Platforms = "sync-igdb-platforms";
    public const string PlatformLinks = "sync-igdb-platform-links";
    public const string Games = "sync-igdb-games";
    public const string GameRelations = "sync-igdb-game-relations";
    public const string InvolvedCompanies = "sync-igdb-involved-companies";
    public const string ExternalGames = "sync-igdb-external-games";
    public const string Popularity = "sync-igdb-popularity";
    public const string Complete = "complete-igdb-sync";
    public const string RefreshPopularity = "refresh-igdb-popularity";

    public static string ForStage(IgdbSyncStageKind stage)
    {
        return stage switch
        {
            IgdbSyncStageKind.GameTypes => GameTypes,
            IgdbSyncStageKind.GameStatuses => GameStatuses,
            IgdbSyncStageKind.Genres => Genres,
            IgdbSyncStageKind.Themes => Themes,
            IgdbSyncStageKind.GameModes => GameModes,
            IgdbSyncStageKind.GameEngines => GameEngines,
            IgdbSyncStageKind.PlayerPerspectives => PlayerPerspectives,
            IgdbSyncStageKind.PlatformTypes => PlatformTypes,
            IgdbSyncStageKind.WebsiteTypes => WebsiteTypes,
            IgdbSyncStageKind.PopularityTypes => PopularityTypes,
            IgdbSyncStageKind.ExternalGameSources => ExternalGameSources,
            IgdbSyncStageKind.Companies => Companies,
            IgdbSyncStageKind.Collections => Collections,
            IgdbSyncStageKind.Franchises => Franchises,
            IgdbSyncStageKind.Platforms => Platforms,
            IgdbSyncStageKind.PlatformLinks => PlatformLinks,
            IgdbSyncStageKind.Games => Games,
            IgdbSyncStageKind.GameRelations => GameRelations,
            IgdbSyncStageKind.InvolvedCompanies => InvolvedCompanies,
            IgdbSyncStageKind.ExternalGames => ExternalGames,
            IgdbSyncStageKind.Popularity => Popularity,
            IgdbSyncStageKind.Complete => Complete,
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported IGDB sync stage."),
        };
    }
}

public sealed record CatalogSyncJobRequest(Guid RunId);

public sealed record CatalogSyncResumeRequest(Guid RunId);
