using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.Games;

public enum GameRelationType
{
    Dlc,
    Expansion,
    StandaloneExpansion,
}

public sealed class GameRelation
{
    public long GameId { get; set; }

    public Game Game { get; set; } = null!;

    public long RelatedGameId { get; set; }

    public Game RelatedGame { get; set; } = null!;

    public GameRelationType RelationType { get; set; }

    public DateTime CreatedAt { get; set; }
}

public sealed class GameRelationConfiguration : IEntityTypeConfiguration<GameRelation>
{
    public void Configure(EntityTypeBuilder<GameRelation> gameRelation)
    {
        gameRelation.ToTable("GameRelations");
        gameRelation.HasKey(value => new
        {
            value.GameId,
            value.RelatedGameId,
            value.RelationType,
        });
        gameRelation.HasIndex(value => value.RelatedGameId);
        gameRelation.Property(value => value.RelationType).HasConversion<string>().HasMaxLength(32);
        gameRelation.Property(value => value.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        gameRelation.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_GameRelations_DifferentGames",
                """
                "GameId" <> "RelatedGameId"
                """
            );
            table.HasCheckConstraint(
                "CK_GameRelations_RelationType",
                """
                "RelationType" IN ('Dlc', 'Expansion', 'StandaloneExpansion')
                """
            );
        });
        gameRelation
            .HasOne(value => value.Game)
            .WithMany()
            .HasForeignKey(value => value.GameId)
            .OnDelete(DeleteBehavior.Cascade);
        gameRelation
            .HasOne(value => value.RelatedGame)
            .WithMany()
            .HasForeignKey(value => value.RelatedGameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
