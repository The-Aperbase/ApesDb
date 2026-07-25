using ApesDb.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.Notifications;

public sealed class Notification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string Type { get; set; } = null!;

    public Guid ResourceId { get; set; }

    public bool IsActionable { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public DateTime? ResolvedAt { get; set; }
}

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> notification)
    {
        notification.HasKey(value => value.Id);
        notification.Property(value => value.Id).HasDefaultValueSql("uuidv7()").ValueGeneratedOnAdd();
        notification.Property(value => value.Type).HasMaxLength(100);
        notification.Property(value => value.CreatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        notification
            .HasIndex(value => new
            {
                value.UserId,
                value.Type,
                value.ResourceId,
            })
            .IsUnique();
        notification.HasIndex(value => new
        {
            value.UserId,
            value.ResolvedAt,
            value.CreatedAt,
        });
        notification
            .HasOne(value => value.User)
            .WithMany()
            .HasForeignKey(value => value.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
