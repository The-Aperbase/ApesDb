using ApesDb.Domain.Entities.Games;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.IgdbSync;

public sealed class IgdbSyncTouchedRelationParent
{
    public Guid RunId { get; set; }

    public IgdbSyncRun Run { get; set; } = null!;

    public long GameId { get; set; }

    public GameRelationType RelationType { get; set; }
}

public sealed class IgdbSyncTouchedRelationParentConfiguration : IEntityTypeConfiguration<IgdbSyncTouchedRelationParent>
{
    public void Configure(EntityTypeBuilder<IgdbSyncTouchedRelationParent> parent)
    {
        parent.ToTable("IgdbSyncTouchedRelationParents");
        parent.HasKey(value => new
        {
            value.RunId,
            value.GameId,
            value.RelationType,
        });
        parent.HasIndex(value => value.GameId);
        parent.Property(value => value.RelationType).HasConversion<string>().HasMaxLength(32);
        parent.ToTable(table =>
            table.HasCheckConstraint(
                "CK_IgdbSyncTouchedRelationParents_RelationType",
                """
                "RelationType" IN ('Dlc', 'Expansion', 'StandaloneExpansion')
                """
            )
        );
        parent
            .HasOne(value => value.Run)
            .WithMany()
            .HasForeignKey(value => value.RunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
