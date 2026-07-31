using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.Boards;

public sealed class BoardInvitationStatus
{
    public const int Pending = 0;
    public const int Accepted = 1;
    public const int Declined = 2;
    public const int Cancelled = 3;
    public const int MaximumNameLength = 16;

    public int Id { get; set; }

    public required string Name { get; set; }
}

public sealed class BoardInvitationStatusConfiguration : IEntityTypeConfiguration<BoardInvitationStatus>
{
    public void Configure(EntityTypeBuilder<BoardInvitationStatus> status)
    {
        status.ToTable("BoardInvitationStatuses");
        status.HasKey(value => value.Id);
        status.Property(value => value.Id).ValueGeneratedNever();
        status.Property(value => value.Name).HasMaxLength(BoardInvitationStatus.MaximumNameLength);
        status.HasIndex(value => value.Name).IsUnique();
    }
}
