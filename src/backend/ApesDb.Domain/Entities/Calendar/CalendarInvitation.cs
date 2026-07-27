using ApesDb.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.Calendar;

public enum CalendarInvitationStatus
{
    Pending,
    Accepted,
    Declined,
    Cancelled,
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

    public CalendarInvitationStatus Status { get; set; }

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
            .HasFilter("\"Status\" = 'Pending'");
        invitation.HasIndex(value => new { value.InviteeUserId, value.Status });
        invitation.Property(value => value.Id).HasDefaultValueSql("uuidv7()").ValueGeneratedOnAdd();
        invitation.Property(value => value.InviteeEmail).HasMaxLength(CalendarInvitation.MaximumEmailLength);
        invitation.Property(value => value.Status).HasConversion<string>().HasMaxLength(16);
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
        invitation.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_CalendarInvitations_Status",
                "\"Status\" IN ('Pending', 'Accepted', 'Declined', 'Cancelled')"
            );
            table.HasCheckConstraint(
                "CK_CalendarInvitations_Resolution",
                "(\"Status\" = 'Pending' AND \"ResolvedAt\" IS NULL) "
                    + "OR (\"Status\" <> 'Pending' AND \"ResolvedAt\" IS NOT NULL)"
            );
        });
    }
}
