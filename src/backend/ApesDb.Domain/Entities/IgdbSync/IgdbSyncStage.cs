using ApesDb.Domain.Entities.Games;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.IgdbSync;

public enum IgdbSyncStageKind
{
    GameTypes,
    GameStatuses,
    Genres,
    Themes,
    GameModes,
    GameEngines,
    PlayerPerspectives,
    PlatformTypes,
    WebsiteTypes,
    PopularityTypes,
    ExternalGameSources,
    Companies,
    Collections,
    Franchises,
    Platforms,
    PlatformLinks,
    Games,
    GameRelations,
    InvolvedCompanies,
    ExternalGames,
    Popularity,
    Complete,
}

public enum IgdbSyncStageStatus
{
    Pending,
    Running,
    Failed,
    Succeeded,
}

public sealed class IgdbSyncStage
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid RunId { get; set; }

    public IgdbSyncRun Run { get; set; } = null!;

    public IgdbSyncStageKind Kind { get; set; }

    public int Order { get; set; }

    public IgdbSyncStageStatus Status { get; set; }

    public long PageCursor { get; set; } = -1;

    public int PagesProcessed { get; set; }

    public long RowsProcessed { get; set; }

    public long RowsSkipped { get; set; }

    public string? Error { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public sealed class IgdbSyncStageConfiguration : IEntityTypeConfiguration<IgdbSyncStage>
{
    public void Configure(EntityTypeBuilder<IgdbSyncStage> stage)
    {
        stage.ToTable("IgdbSyncStages");
        stage.HasKey(value => value.Id);
        stage.Property(value => value.Id).HasDefaultValueSql("uuidv7()").ValueGeneratedOnAdd();
        stage.Property(value => value.Kind).HasConversion<string>().HasMaxLength(64);
        stage.Property(value => value.Status).HasConversion<string>().HasMaxLength(32);
        stage.Property(value => value.PageCursor).HasDefaultValue(-1L);
        stage.Property(value => value.RowsSkipped).HasDefaultValue(0L);
        stage.Property(value => value.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        stage.Property(value => value.UpdatedAt).HasDefaultValueSql("now()");
        stage.HasIndex(value => new { value.RunId, value.Kind }).IsUnique();
        stage.HasIndex(value => new { value.RunId, value.Order }).IsUnique();
        stage.HasIndex(value => value.Status);
        stage.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_IgdbSyncStages_Status",
                """
                "Status" IN ('Pending', 'Running', 'Failed', 'Succeeded')
                """
            );
            table.HasCheckConstraint(
                "CK_IgdbSyncStages_Order",
                """
                "Order" >= 0
                """
            );
            table.HasCheckConstraint(
                "CK_IgdbSyncStages_PageCursor",
                """
                "PageCursor" >= -1
                """
            );
            table.HasCheckConstraint(
                "CK_IgdbSyncStages_PagesProcessed",
                """
                "PagesProcessed" >= 0
                """
            );
            table.HasCheckConstraint(
                "CK_IgdbSyncStages_RowsProcessed",
                """
                "RowsProcessed" >= 0
                """
            );
            table.HasCheckConstraint(
                "CK_IgdbSyncStages_RowsSkipped",
                """
                "RowsSkipped" >= 0
                """
            );
            table.HasCheckConstraint(
                "CK_IgdbSyncStages_Completion",
                """
                ("Status" = 'Succeeded' AND "CompletedAt" IS NOT NULL) OR "Status" <> 'Succeeded'
                """
            );
        });
        stage
            .HasOne(value => value.Run)
            .WithMany()
            .HasForeignKey(value => value.RunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
