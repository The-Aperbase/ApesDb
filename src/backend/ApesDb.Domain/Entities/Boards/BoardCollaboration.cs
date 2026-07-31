using ApesDb.Domain.Entities.Users;
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

public sealed class BoardInvitation
{
    public const int MaximumEmailLength = 256;

    public Guid Id { get; set; }

    public Guid BoardId { get; set; }

    public Board Board { get; set; } = null!;

    public Guid? InviteeUserId { get; set; }

    public User? InviteeUser { get; set; }

    public required string InviteeEmail { get; set; }

    public int StatusId { get; set; }

    public BoardInvitationStatus Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }
}

public sealed class BoardCollaborator
{
    public Guid BoardId { get; set; }

    public Board Board { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime JoinedAt { get; set; }
}

public sealed class BoardInvitationConfiguration : IEntityTypeConfiguration<BoardInvitation>
{
    public void Configure(EntityTypeBuilder<BoardInvitation> invitation)
    {
        invitation.HasKey(value => value.Id);
        invitation
            .HasIndex(value => new { value.BoardId, value.InviteeEmail })
            .IsUnique()
            .HasFilter($"\"StatusId\" = {BoardInvitationStatus.Pending}");
        invitation.HasIndex(value => new { value.InviteeUserId, value.StatusId });
        invitation.Property(value => value.Id).HasDefaultValueSql("uuidv7()").ValueGeneratedOnAdd();
        invitation.Property(value => value.InviteeEmail).HasMaxLength(BoardInvitation.MaximumEmailLength);
        invitation.Property(value => value.CreatedAt).HasDefaultValueSql("now()");
        invitation
            .HasOne(value => value.Board)
            .WithMany()
            .HasForeignKey(value => value.BoardId)
            .OnDelete(DeleteBehavior.Cascade);
        invitation
            .HasOne(value => value.InviteeUser)
            .WithMany()
            .HasForeignKey(value => value.InviteeUserId)
            .OnDelete(DeleteBehavior.Cascade);
        invitation
            .HasOne(value => value.Status)
            .WithMany()
            .HasForeignKey(value => value.StatusId)
            .OnDelete(DeleteBehavior.Restrict);
        invitation.ToTable(table =>
            table.HasCheckConstraint(
                "CK_BoardInvitations_Resolution",
                $"(\"StatusId\" = {BoardInvitationStatus.Pending} AND \"ResolvedAt\" IS NULL) OR (\"StatusId\" <> {BoardInvitationStatus.Pending} AND \"ResolvedAt\" IS NOT NULL)"
            )
        );
    }
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

public sealed class BoardCollaboratorConfiguration : IEntityTypeConfiguration<BoardCollaborator>
{
    public void Configure(EntityTypeBuilder<BoardCollaborator> collaborator)
    {
        collaborator.HasKey(value => new { value.BoardId, value.UserId });
        collaborator.HasIndex(value => value.UserId);
        collaborator.Property(value => value.JoinedAt).HasDefaultValueSql("now()");
        collaborator
            .HasOne(value => value.Board)
            .WithMany()
            .HasForeignKey(value => value.BoardId)
            .OnDelete(DeleteBehavior.Cascade);
        collaborator
            .HasOne(value => value.User)
            .WithMany()
            .HasForeignKey(value => value.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
