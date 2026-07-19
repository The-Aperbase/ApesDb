using ApesDb.Domain.Entities.Teams;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.GamesLists;

public sealed class GamesList
{
    public Guid Id { get; init; }

    public Guid TeamId { get; init; }

    public Team Team { get; init; } = null!;

    public required string Name { get; set; }

    public byte[]? Picture { get; set; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; set; }

    public List<GamesListEntry> Entries { get; } = [];
}

public sealed class GamesListConfiguration : IEntityTypeConfiguration<GamesList>
{
    public void Configure(EntityTypeBuilder<GamesList> gamesList)
    {
        gamesList.HasKey(value => value.Id);
        gamesList.HasIndex(value => value.TeamId);
        gamesList.Property(value => value.Id).HasDefaultValueSql("uuidv7()").ValueGeneratedOnAdd();
        gamesList.Property(value => value.Name).HasMaxLength(256);
        gamesList.Property(value => value.CreatedAt).HasDefaultValueSql("now()");
        gamesList.Property(value => value.UpdatedAt).HasDefaultValueSql("now()");
        gamesList
            .HasOne(value => value.Team)
            .WithMany()
            .HasForeignKey(value => value.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
