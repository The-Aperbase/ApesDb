using ApesDb.Domain.Entities.Games;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.IgdbSync;

public sealed class IgdbSyncPendingGameRelation
{
    public Guid RunId { get; set; }

    public IgdbSyncRun Run { get; set; } = null!;

    public long GameId { get; set; }

    public long RelatedGameId { get; set; }

    public GameRelationType RelationType { get; set; }
}

public sealed class IgdbSyncPendingGameRelationConfiguration : IEntityTypeConfiguration<IgdbSyncPendingGameRelation>
{
    public void Configure(EntityTypeBuilder<IgdbSyncPendingGameRelation> relation)
    {
        relation.ToTable("IgdbSyncPendingGameRelations");
        relation.HasKey(value => new
        {
            value.RunId,
            value.GameId,
            value.RelatedGameId,
            value.RelationType,
        });
        relation.HasIndex(value => new { value.RunId, value.RelatedGameId });
        relation.Property(value => value.RelationType).HasConversion<string>().HasMaxLength(32);
        relation.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_IgdbSyncPendingGameRelations_DifferentGames",
                """
                "GameId" <> "RelatedGameId"
                """
            );
            table.HasCheckConstraint(
                "CK_IgdbSyncPendingGameRelations_RelationType",
                """
                "RelationType" IN ('Dlc', 'Expansion', 'StandaloneExpansion')
                """
            );
        });
        relation
            .HasOne(value => value.Run)
            .WithMany()
            .HasForeignKey(value => value.RunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
