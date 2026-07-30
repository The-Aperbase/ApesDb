using ApesDb.Domain.Entities.Games;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.IgdbSync;

public enum IgdbSyncRunMode
{
    Bootstrap,
    Incremental,
}

public enum IgdbSyncRunStatus
{
    Pending,
    Running,
    Failed,
    Succeeded,
}

public sealed class IgdbSyncRun
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public IgdbSyncRunMode Mode { get; set; }

    public IgdbSyncRunStatus Status { get; set; }

    public DateTime? From { get; set; }

    public DateTime Through { get; set; }

    public long RowsProcessed { get; set; }

    public long RowsSkipped { get; set; }

    public string? Error { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public sealed class IgdbSyncRunConfiguration : IEntityTypeConfiguration<IgdbSyncRun>
{
    public void Configure(EntityTypeBuilder<IgdbSyncRun> run)
    {
        run.ToTable("IgdbSyncRuns");
        run.HasKey(value => value.Id);
        run.Property(value => value.Id).HasDefaultValueSql("uuidv7()").ValueGeneratedOnAdd();
        run.Property(value => value.Mode).HasConversion<string>().HasMaxLength(32);
        run.Property(value => value.Status).HasConversion<string>().HasMaxLength(32);
        run.Property(value => value.RowsSkipped).HasDefaultValue(0L);
        run.Property(value => value.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        run.Property(value => value.UpdatedAt).HasDefaultValueSql("now()");
        run.Property<int>("CatalogLock").HasDefaultValue(1).ValueGeneratedOnAdd();
        run.HasIndex("CatalogLock")
            .IsUnique()
            .HasDatabaseName("UX_IgdbSyncRuns_Unfinished")
            .HasFilter(
                """
                "Status" <> 'Succeeded'
                """
            );
        run.HasIndex(value => new { value.Status, value.Through });
        run.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_IgdbSyncRuns_Mode",
                """
                "Mode" IN ('Bootstrap', 'Incremental')
                """
            );
            table.HasCheckConstraint(
                "CK_IgdbSyncRuns_Status",
                """
                "Status" IN ('Pending', 'Running', 'Failed', 'Succeeded')
                """
            );
            table.HasCheckConstraint(
                "CK_IgdbSyncRuns_CatalogLock",
                """
                "CatalogLock" = 1
                """
            );
            table.HasCheckConstraint(
                "CK_IgdbSyncRuns_Window",
                """
                ("Mode" = 'Bootstrap' AND "From" IS NULL) OR ("Mode" = 'Incremental' AND "From" IS NOT NULL AND "From" < "Through")
                """
            );
            table.HasCheckConstraint(
                "CK_IgdbSyncRuns_RowsProcessed",
                """
                "RowsProcessed" >= 0
                """
            );
            table.HasCheckConstraint(
                "CK_IgdbSyncRuns_RowsSkipped",
                """
                "RowsSkipped" >= 0
                """
            );
            table.HasCheckConstraint(
                "CK_IgdbSyncRuns_Completion",
                """
                ("Status" = 'Succeeded' AND "CompletedAt" IS NOT NULL) OR "Status" <> 'Succeeded'
                """
            );
        });
    }
}
