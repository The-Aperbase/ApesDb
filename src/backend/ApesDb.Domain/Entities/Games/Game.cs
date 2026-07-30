using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.Games;

public sealed class Game : IIgdbEntity
{
    public long Id { get; set; }

    public required string Name { get; set; }

    public string? Slug { get; set; }

    public string? Summary { get; set; }

    public string? Storyline { get; set; }

    public DateTime? FirstReleaseDate { get; set; }

    public decimal? TotalRating { get; set; }

    public long? TotalRatingCount { get; set; }

    public string? IgdbUrl { get; set; }

    public long? GameTypeId { get; set; }

    public GameType? GameType { get; set; }

    public long? GameStatusId { get; set; }

    public GameStatus? GameStatus { get; set; }

    public long? VersionParentId { get; set; }

    public string? CoverImageId { get; set; }

    public int? CoverWidth { get; set; }

    public int? CoverHeight { get; set; }

    public string? CoverSmallUrl { get; set; }

    public string? CoverLargeUrl { get; set; }

    public Guid? Checksum { get; set; }

    public DateTime? IgdbUpdatedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime LastSyncedAt { get; set; }
}

public sealed class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> game)
    {
        game.ToTable("Games");
        game.ConfigureIgdbEntity();
        game.Property(value => value.Name).HasMaxLength(512);
        game.Property(value => value.Slug).HasMaxLength(512);
        game.Property(value => value.TotalRating).HasPrecision(8, 4);
        game.Property(value => value.IgdbUrl).HasMaxLength(2048);
        game.Property(value => value.CoverImageId).HasMaxLength(128);
        game.Property(value => value.CoverSmallUrl).HasMaxLength(2048);
        game.Property(value => value.CoverLargeUrl).HasMaxLength(2048);
        game.HasIndex(value => value.VersionParentId);
        game.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_Games_TotalRating",
                """
                "TotalRating" IS NULL OR ("TotalRating" >= 0 AND "TotalRating" <= 100)
                """
            );
            table.HasCheckConstraint(
                "CK_Games_TotalRatingCount",
                """
                "TotalRatingCount" IS NULL OR "TotalRatingCount" >= 0
                """
            );
            table.HasCheckConstraint(
                "CK_Games_CoverWidth",
                """
                "CoverWidth" IS NULL OR "CoverWidth" > 0
                """
            );
            table.HasCheckConstraint(
                "CK_Games_CoverHeight",
                """
                "CoverHeight" IS NULL OR "CoverHeight" > 0
                """
            );
        });
        game.HasOne(value => value.GameType)
            .WithMany()
            .HasForeignKey(value => value.GameTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        game.HasOne(value => value.GameStatus)
            .WithMany()
            .HasForeignKey(value => value.GameStatusId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
