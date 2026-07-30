using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.Games;

public sealed class Company : IIgdbEntity
{
    public long Id { get; set; }

    public required string Name { get; set; }

    public string? Slug { get; set; }

    public string? Description { get; set; }

    public int? CountryCode { get; set; }

    public string? IgdbUrl { get; set; }

    public string? LogoImageId { get; set; }

    public int? LogoWidth { get; set; }

    public int? LogoHeight { get; set; }

    public Guid? Checksum { get; set; }

    public DateTime? IgdbUpdatedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime LastSyncedAt { get; set; }
}

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> company)
    {
        company.ToTable("Companies");
        company.ConfigureIgdbEntity();
        company.Property(value => value.Name).HasMaxLength(512);
        company.Property(value => value.Slug).HasMaxLength(512);
        company.Property(value => value.IgdbUrl).HasMaxLength(2048);
        company.Property(value => value.LogoImageId).HasMaxLength(128);
        company.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_Companies_LogoWidth",
                """
                "LogoWidth" IS NULL OR "LogoWidth" > 0
                """
            );
            table.HasCheckConstraint(
                "CK_Companies_LogoHeight",
                """
                "LogoHeight" IS NULL OR "LogoHeight" > 0
                """
            );
        });
    }
}
