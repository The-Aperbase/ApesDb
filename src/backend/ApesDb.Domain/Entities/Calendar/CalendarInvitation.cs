using ApesDb.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.Calendar;

public sealed class CalendarInvitationStatus
{
    public const int Pending = 0;
    public const int Accepted = 1;
    public const int Declined = 2;
    public const int Cancelled = 3;
    public const int MaximumNameLength = 16;

    public int Id { get; set; }

    public required string Name { get; set; }
}

public sealed class CalendarInvitation
{
    public const int MaximumEmailLength = 256;

    public Guid Id { get; set; }

    public Guid InviterUserId { get; set; }

    public User InviterUser { get; set; } = null!;

    public Guid? InviteeUserId { get; set; }

    public User? InviteeUser { get; set; }

    public required string InviteeEmail { get; set; }

    public int StatusId { get; set; }

    public CalendarInvitationStatus Status { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }
}

public sealed class CalendarInvitationConfiguration : IEntityTypeConfiguration<CalendarInvitation>
{
    public void Configure(EntityTypeBuilder<CalendarInvitation> invitation)
    {
        invitation.HasKey(value => value.Id);
        invitation
            .HasIndex(value => new { value.InviterUserId, value.InviteeEmail })
            .IsUnique()
            .HasFilter(
                $"""
                "StatusId" = {CalendarInvitationStatus.Pending}
                """
            );
        invitation.HasIndex(value => new { value.InviteeUserId, value.StatusId });
        invitation.HasIndex(value => value.StatusId);
        invitation.Property(value => value.Id).HasDefaultValueSql("uuidv7()").ValueGeneratedOnAdd();
        invitation.Property(value => value.InviteeEmail).HasMaxLength(CalendarInvitation.MaximumEmailLength);
        invitation.Property(value => value.CreatedAt).HasDefaultValueSql("now()");
        invitation
            .HasOne(value => value.InviterUser)
            .WithMany()
            .HasForeignKey(value => value.InviterUserId)
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
        {
            table.HasCheckConstraint(
                "CK_CalendarInvitations_Resolution",
                $"""
                ("StatusId" = {CalendarInvitationStatus.Pending} AND "ResolvedAt" IS NULL) OR ("StatusId" <> {CalendarInvitationStatus.Pending} AND "ResolvedAt" IS NOT NULL)
                """
            );
        });
    }
}

public sealed class CalendarInvitationStatusConfiguration : IEntityTypeConfiguration<CalendarInvitationStatus>
{
    public void Configure(EntityTypeBuilder<CalendarInvitationStatus> status)
    {
        status.ToTable("CalendarInvitationStatuses");
        status.HasKey(value => value.Id);
        status.Property(value => value.Id).ValueGeneratedNever();
        status.Property(value => value.Name).HasMaxLength(CalendarInvitationStatus.MaximumNameLength);
        status.HasIndex(value => value.Name).IsUnique();
    }
}
