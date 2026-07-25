using ApesDb.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.Boards;

public sealed class Board
{
    public const int MaximumNameLength = 128;

    public Guid Id { get; init; }

    public Guid OwnerUserId { get; init; }

    public User OwnerUser { get; init; } = null!;

    public required string Name { get; set; }

    public byte[]? Picture { get; set; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; set; }

    public List<BoardEntry> Entries { get; } = [];
}

public sealed class BoardConfiguration : IEntityTypeConfiguration<Board>
{
    public void Configure(EntityTypeBuilder<Board> board)
    {
        board.HasKey(value => value.Id);
        board.HasIndex(value => value.OwnerUserId);
        board.Property(value => value.Id).HasDefaultValueSql("uuidv7()").ValueGeneratedOnAdd();
        board.Property(value => value.Name).HasMaxLength(Board.MaximumNameLength);
        board.Property(value => value.CreatedAt).HasDefaultValueSql("now()");
        board.Property(value => value.UpdatedAt).HasDefaultValueSql("now()");
        board
            .HasOne(value => value.OwnerUser)
            .WithMany()
            .HasForeignKey(value => value.OwnerUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
