using ApesDb.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.Calendar;

public sealed class CalendarEvent
{
    public const int MaximumTitleLength = 128;
    public const int MaximumTimeZoneIdLength = 128;

    public Guid Id { get; set; }

    public Guid OwnerUserId { get; set; }

    public User OwnerUser { get; set; } = null!;

    public required string Title { get; set; }

    public DateTimeOffset StartAt { get; set; }

    public DateTimeOffset EndAt { get; set; }

    public bool AllDay { get; set; }

    public required string TimeZoneId { get; set; }

    public string? RecurrenceJson { get; set; }

    public DateTimeOffset? RecurrenceUntil { get; set; }

    public Guid? RecurringEventId { get; set; }

    public CalendarEvent? RecurringEvent { get; set; }

    public DateTimeOffset? OriginalStartAt { get; set; }

    public bool IsCancelled { get; set; }

    public bool TitleOverridden { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class CalendarEventConfiguration : IEntityTypeConfiguration<CalendarEvent>
{
    public void Configure(EntityTypeBuilder<CalendarEvent> calendarEvent)
    {
        calendarEvent.HasKey(value => value.Id);
        calendarEvent.HasIndex(value => new
        {
            value.OwnerUserId,
            value.StartAt,
            value.EndAt,
        });
        calendarEvent.HasIndex(value => value.RecurringEventId);
        calendarEvent
            .HasIndex(value => new { value.RecurringEventId, value.OriginalStartAt })
            .IsUnique()
            .HasFilter("\"RecurringEventId\" IS NOT NULL");
        calendarEvent.Property(value => value.Id).HasDefaultValueSql("uuidv7()").ValueGeneratedOnAdd();
        calendarEvent.Property(value => value.Title).HasMaxLength(CalendarEvent.MaximumTitleLength);
        calendarEvent.Property(value => value.TimeZoneId).HasMaxLength(CalendarEvent.MaximumTimeZoneIdLength);
        calendarEvent.Property(value => value.RecurrenceJson).HasColumnType("jsonb");
        calendarEvent.Property(value => value.CreatedAt).HasDefaultValueSql("now()");
        calendarEvent.Property(value => value.UpdatedAt).HasDefaultValueSql("now()");
        calendarEvent
            .HasOne(value => value.OwnerUser)
            .WithMany()
            .HasForeignKey(value => value.OwnerUserId)
            .OnDelete(DeleteBehavior.Cascade);
        calendarEvent
            .HasOne(value => value.RecurringEvent)
            .WithMany()
            .HasForeignKey(value => value.RecurringEventId)
            .OnDelete(DeleteBehavior.Cascade);
        calendarEvent.ToTable(table =>
        {
            table.HasCheckConstraint("CK_CalendarEvents_Duration", "\"EndAt\" > \"StartAt\"");
            table.HasCheckConstraint(
                "CK_CalendarEvents_Exception",
                "(\"RecurringEventId\" IS NULL AND \"OriginalStartAt\" IS NULL AND \"IsCancelled\" = false) "
                    + "OR (\"RecurringEventId\" IS NOT NULL AND \"OriginalStartAt\" IS NOT NULL "
                    + "AND \"RecurrenceJson\" IS NULL)"
            );
        });
    }
}
