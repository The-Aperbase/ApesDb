using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.Games;

public sealed class Platform : IIgdbEntity
{
    public long Id { get; set; }

    public required string Name { get; set; }

    public string? Abbreviation { get; set; }

    public string? AlternativeName { get; set; }

    public string? Slug { get; set; }

    public string? Summary { get; set; }

    public string? IgdbUrl { get; set; }

    public long? PlatformTypeId { get; set; }

    public PlatformType? PlatformType { get; set; }

    public int? Generation { get; set; }

    public string? LogoImageId { get; set; }

    public int? LogoWidth { get; set; }

    public int? LogoHeight { get; set; }

    public Guid? Checksum { get; set; }

    public DateTime? IgdbUpdatedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime LastSyncedAt { get; set; }
}

public sealed class PlatformConfiguration : IEntityTypeConfiguration<Platform>
{
    public void Configure(EntityTypeBuilder<Platform> platform)
    {
        platform.ToTable("Platforms");
        platform.ConfigureIgdbEntity();
        platform.HasIndex(value => value.PlatformTypeId);
        platform.Property(value => value.Name).HasMaxLength(256);
        platform.Property(value => value.Abbreviation).HasMaxLength(128);
        platform.Property(value => value.AlternativeName).HasMaxLength(256);
        platform.Property(value => value.Slug).HasMaxLength(256);
        platform.Property(value => value.IgdbUrl).HasMaxLength(2048);
        platform.Property(value => value.LogoImageId).HasMaxLength(128);
        platform.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_Platforms_Generation",
                """
                "Generation" IS NULL OR "Generation" >= 0
                """
            );
            table.HasCheckConstraint(
                "CK_Platforms_LogoWidth",
                """
                "LogoWidth" IS NULL OR "LogoWidth" > 0
                """
            );
            table.HasCheckConstraint(
                "CK_Platforms_LogoHeight",
                """
                "LogoHeight" IS NULL OR "LogoHeight" > 0
                """
            );
        });
        platform
            .HasOne(value => value.PlatformType)
            .WithMany()
            .HasForeignKey(value => value.PlatformTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
