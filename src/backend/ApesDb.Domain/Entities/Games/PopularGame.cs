using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.Games;

public sealed class PopularGame
{
    public long Id { get; set; }

    public long GameId { get; set; }

    public Game Game { get; set; } = null!;

    public int Rank { get; set; }

    public int SourceRank { get; set; }

    public decimal Score { get; set; }

    public long PopularityTypeId { get; set; }

    public PopularityType PopularityType { get; set; } = null!;

    public DateTime CalculatedAt { get; set; }

    public DateTime? IgdbUpdatedAt { get; set; }

    public Guid? Checksum { get; set; }

    public DateTime SyncedAt { get; set; }
}

public sealed class PopularGameConfiguration : IEntityTypeConfiguration<PopularGame>
{
    public void Configure(EntityTypeBuilder<PopularGame> popularGame)
    {
        popularGame.ToTable("PopularGames");
        popularGame.HasKey(value => value.Id);
        popularGame.Property(value => value.Id).ValueGeneratedNever();
        popularGame.HasIndex(value => value.GameId).IsUnique();
        popularGame.HasIndex(value => value.Rank).IsUnique();
        popularGame.HasIndex(value => value.PopularityTypeId);
        popularGame.Property(value => value.Score).HasPrecision(28, 18);
        popularGame.Property(value => value.SyncedAt).HasDefaultValueSql("now()");
        popularGame.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_PopularGames_Rank",
                """
                "Rank" BETWEEN 1 AND 1000
                """
            );
            table.HasCheckConstraint(
                "CK_PopularGames_SourceRank",
                """
                "SourceRank" > 0
                """
            );
            table.HasCheckConstraint(
                "CK_PopularGames_Score",
                """
                "Score" >= 0
                """
            );
        });
        popularGame
            .HasOne(value => value.Game)
            .WithOne()
            .HasForeignKey<PopularGame>(value => value.GameId)
            .OnDelete(DeleteBehavior.Cascade);
        popularGame
            .HasOne(value => value.PopularityType)
            .WithMany()
            .HasForeignKey(value => value.PopularityTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
