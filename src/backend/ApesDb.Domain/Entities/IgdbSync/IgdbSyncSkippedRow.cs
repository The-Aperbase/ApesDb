using ApesDb.Domain.Entities.Games;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.IgdbSync;

public enum IgdbSyncSkipReason
{
    MissingGame,
    MissingCompany,
    MissingExternalGameSource,
    MissingPlatform,
}

public sealed class IgdbSyncSkippedRow
{
    public Guid StageId { get; set; }

    public long EntityId { get; set; }

    public IgdbSyncSkipReason Reason { get; set; }

    public long MissingDependencyId { get; set; }

    public DateTime CreatedAt { get; set; }
}

public sealed class IgdbSyncSkippedRowConfiguration : IEntityTypeConfiguration<IgdbSyncSkippedRow>
{
    public void Configure(EntityTypeBuilder<IgdbSyncSkippedRow> skippedRow)
    {
        skippedRow.ToTable("IgdbSyncSkippedRows");
        skippedRow.HasKey(value => new
        {
            value.StageId,
            value.EntityId,
            value.Reason,
            value.MissingDependencyId,
        });
        skippedRow.Property(value => value.Reason).HasConversion<string>().HasMaxLength(64);
        skippedRow.Property(value => value.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        skippedRow.HasIndex(value => value.EntityId);
        skippedRow.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_IgdbSyncSkippedRows_Reason",
                """
                "Reason" IN ('MissingGame', 'MissingCompany', 'MissingExternalGameSource', 'MissingPlatform')
                """
            );
        });
        skippedRow
            .HasOne<IgdbSyncStage>()
            .WithMany()
            .HasForeignKey(value => value.StageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
