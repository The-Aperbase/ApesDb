using ApesDb.Domain.Entities.Games;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.GamesLists;

public sealed class GamesListEntry
{
    public Guid GamesListId { get; init; }

    public GamesList GamesList { get; init; } = null!;

    public long GameId { get; init; }

    public Game Game { get; init; } = null!;

    public DateTime AddedAt { get; init; }
}

public sealed class GamesListEntryConfiguration : IEntityTypeConfiguration<GamesListEntry>
{
    public void Configure(EntityTypeBuilder<GamesListEntry> entry)
    {
        entry.HasKey(value => new { value.GamesListId, value.GameId });
        entry.HasIndex(value => value.GameId);
        entry.Property(value => value.AddedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        entry
            .HasOne(value => value.GamesList)
            .WithMany(value => value.Entries)
            .HasForeignKey(value => value.GamesListId)
            .OnDelete(DeleteBehavior.Cascade);
        entry
            .HasOne(value => value.Game)
            .WithMany()
            .HasForeignKey(value => value.GameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
