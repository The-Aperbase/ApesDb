using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.Games;

public sealed class ExternalGame : IIgdbEntity
{
    public long Id { get; set; }

    public long GameId { get; set; }

    public Game Game { get; set; } = null!;

    public long ExternalGameSourceId { get; set; }

    public ExternalGameSource ExternalGameSource { get; set; } = null!;

    public long? PlatformId { get; set; }

    public Platform? Platform { get; set; }

    public string? ExternalId { get; set; }

    public string? Name { get; set; }

    public string? Url { get; set; }

    public int? Year { get; set; }

    public Guid? Checksum { get; set; }

    public DateTime? IgdbUpdatedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime LastSyncedAt { get; set; }
}

public sealed class ExternalGameConfiguration : IEntityTypeConfiguration<ExternalGame>
{
    public void Configure(EntityTypeBuilder<ExternalGame> externalGame)
    {
        externalGame.ToTable("ExternalGames");
        externalGame.ConfigureIgdbEntity();
        externalGame.HasIndex(value => value.GameId);
        externalGame.HasIndex(value => value.ExternalGameSourceId);
        externalGame.HasIndex(value => value.PlatformId);
        externalGame.HasIndex(value => new { value.ExternalGameSourceId, value.ExternalId });
        externalGame.Property(value => value.ExternalId).HasMaxLength(512);
        externalGame.Property(value => value.Name).HasMaxLength(512);
        externalGame.Property(value => value.Url).HasMaxLength(2048);
        externalGame.ToTable(table =>
            table.HasCheckConstraint(
                "CK_ExternalGames_Year",
                """
                "Year" IS NULL OR "Year" BETWEEN 0 AND 9999
                """
            )
        );
        externalGame
            .HasOne(value => value.Game)
            .WithMany()
            .HasForeignKey(value => value.GameId)
            .OnDelete(DeleteBehavior.Cascade);
        externalGame
            .HasOne(value => value.ExternalGameSource)
            .WithMany()
            .HasForeignKey(value => value.ExternalGameSourceId)
            .HasConstraintName("FK_ExternalGames_ExternalGameSources_ExternalGameSourceId")
            .OnDelete(DeleteBehavior.Restrict);
        externalGame
            .HasOne(value => value.Platform)
            .WithMany()
            .HasForeignKey(value => value.PlatformId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
