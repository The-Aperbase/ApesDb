using ApesDb.Domain.Entities.Games;
using ApesDb.Domain.Entities.IgdbSync;
using ApesDb.Domain.Entities.Notifications;
using ApesDb.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Domain;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();

    public DbSet<AllowedUser> AllowedUsers => Set<AllowedUser>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<Game> Games => Set<Game>();

    public DbSet<GameType> GameTypes => Set<GameType>();

    public DbSet<GameStatus> GameStatuses => Set<GameStatus>();

    public DbSet<PopularityType> PopularityTypes => Set<PopularityType>();

    public DbSet<PopularGame> PopularGames => Set<PopularGame>();

    public DbSet<GameRelation> GameRelations => Set<GameRelation>();

    public DbSet<Genre> Genres => Set<Genre>();

    public DbSet<GameGenre> GameGenres => Set<GameGenre>();

    public DbSet<Theme> Themes => Set<Theme>();

    public DbSet<GameTheme> GameThemes => Set<GameTheme>();

    public DbSet<GameMode> GameModes => Set<GameMode>();

    public DbSet<GameGameMode> GameGameModes => Set<GameGameMode>();

    public DbSet<GameEngine> GameEngines => Set<GameEngine>();

    public DbSet<GameGameEngine> GameGameEngines => Set<GameGameEngine>();

    public DbSet<PlayerPerspective> PlayerPerspectives => Set<PlayerPerspective>();

    public DbSet<GamePlayerPerspective> GamePlayerPerspectives => Set<GamePlayerPerspective>();

    public DbSet<PlatformType> PlatformTypes => Set<PlatformType>();

    public DbSet<WebsiteType> WebsiteTypes => Set<WebsiteType>();

    public DbSet<Platform> Platforms => Set<Platform>();

    public DbSet<GamePlatform> GamePlatforms => Set<GamePlatform>();

    public DbSet<PlatformLink> PlatformLinks => Set<PlatformLink>();

    public DbSet<ExternalGameSource> ExternalGameSources => Set<ExternalGameSource>();

    public DbSet<ExternalGame> ExternalGames => Set<ExternalGame>();

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<GameCompany> GameCompanies => Set<GameCompany>();

    public DbSet<Collection> Collections => Set<Collection>();

    public DbSet<GameCollection> GameCollections => Set<GameCollection>();

    public DbSet<Franchise> Franchises => Set<Franchise>();

    public DbSet<GameFranchise> GameFranchises => Set<GameFranchise>();

    public DbSet<IgdbSyncRun> IgdbSyncRuns => Set<IgdbSyncRun>();

    public DbSet<IgdbSyncStage> IgdbSyncStages => Set<IgdbSyncStage>();

    public DbSet<IgdbSyncSkippedRow> IgdbSyncSkippedRows => Set<IgdbSyncSkippedRow>();

    public DbSet<IgdbSyncTouchedRelationParent> IgdbSyncTouchedRelationParents => Set<IgdbSyncTouchedRelationParent>();

    public DbSet<IgdbSyncPendingGameRelation> IgdbSyncPendingGameRelations => Set<IgdbSyncPendingGameRelation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
