using ApesDb.Domain.Entities.Games;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.Boards;

public sealed class BoardEntryState
{
    public int Id { get; set; }

    public required string Name { get; set; }
}

public sealed class BoardEntry
{
    public Guid BoardId { get; init; }

    public Board Board { get; init; } = null!;

    public long GameId { get; init; }

    public Game Game { get; init; } = null!;

    public int StateId { get; set; }

    public BoardEntryState State { get; set; } = null!;

    public DateTime AddedAt { get; init; }
}

public sealed class BoardEntryConfiguration : IEntityTypeConfiguration<BoardEntry>
{
    public void Configure(EntityTypeBuilder<BoardEntry> entry)
    {
        entry.HasKey(value => new { value.BoardId, value.GameId });
        entry.HasIndex(value => value.GameId);
        entry.Property(value => value.AddedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        entry
            .HasOne(value => value.Board)
            .WithMany(value => value.Entries)
            .HasForeignKey(value => value.BoardId)
            .OnDelete(DeleteBehavior.Cascade);
        entry
            .HasOne(value => value.Game)
            .WithMany()
            .HasForeignKey(value => value.GameId)
            .OnDelete(DeleteBehavior.Cascade);
        entry.HasOne(value => value.State).WithMany().HasForeignKey(value => value.StateId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class BoardEntryStateConfiguration : IEntityTypeConfiguration<BoardEntryState>
{
    public void Configure(EntityTypeBuilder<BoardEntryState> state)
    {
        state.ToTable("BoardEntryStates");
        state.HasKey(value => value.Id);
        state.Property(value => value.Id).ValueGeneratedOnAdd();
        state.Property(value => value.Name).HasMaxLength(16);
        state.HasIndex(value => value.Name).IsUnique();
    }
}
