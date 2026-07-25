using ApesDb.Domain.Entities.Games;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.Boards;

public enum BoardEntryState
{
    Todo = 0,
    InProgress = 1,
    Completed = 2,
    Dnf = 3,
}

public sealed class BoardEntry
{
    public Guid BoardId { get; init; }

    public Board Board { get; init; } = null!;

    public long GameId { get; init; }

    public Game Game { get; init; } = null!;

    public BoardEntryState State { get; set; }

    public DateTime AddedAt { get; init; }
}

public sealed class BoardEntryConfiguration : IEntityTypeConfiguration<BoardEntry>
{
    public void Configure(EntityTypeBuilder<BoardEntry> entry)
    {
        entry.HasKey(value => new { value.BoardId, value.GameId });
        entry.HasIndex(value => value.GameId);
        entry.Property(value => value.State).HasConversion<string>().HasMaxLength(16);
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
        entry.ToTable(table =>
            table.HasCheckConstraint("CK_BoardEntries_State", "\"State\" IN ('Todo', 'InProgress', 'Completed', 'Dnf')")
        );
    }
}
