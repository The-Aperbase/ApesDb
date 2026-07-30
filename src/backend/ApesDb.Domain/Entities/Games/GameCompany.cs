using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.Games;

public sealed class GameCompany : IIgdbEntity
{
    public long Id { get; set; }

    public long GameId { get; set; }

    public Game Game { get; set; } = null!;

    public long CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public bool Developer { get; set; }

    public bool Publisher { get; set; }

    public bool Porting { get; set; }

    public bool Supporting { get; set; }

    public Guid? Checksum { get; set; }

    public DateTime? IgdbUpdatedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime LastSyncedAt { get; set; }
}

public sealed class GameCompanyConfiguration : IEntityTypeConfiguration<GameCompany>
{
    public void Configure(EntityTypeBuilder<GameCompany> gameCompany)
    {
        gameCompany.ToTable("GameCompanies");
        gameCompany.ConfigureIgdbEntity();
        gameCompany.HasIndex(value => value.GameId);
        gameCompany.HasIndex(value => value.CompanyId);
        gameCompany.ToTable(table =>
            table.HasCheckConstraint(
                "CK_GameCompanies_Role",
                """
                "Developer" OR "Publisher" OR "Porting" OR "Supporting"
                """
            )
        );
        gameCompany
            .HasOne(value => value.Game)
            .WithMany()
            .HasForeignKey(value => value.GameId)
            .OnDelete(DeleteBehavior.Cascade);
        gameCompany
            .HasOne(value => value.Company)
            .WithMany()
            .HasForeignKey(value => value.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
