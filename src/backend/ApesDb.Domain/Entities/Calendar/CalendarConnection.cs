using ApesDb.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.Calendar;

public sealed class CalendarConnection
{
    public Guid Id { get; set; }

    public Guid FirstUserId { get; set; }

    public User FirstUser { get; set; } = null!;

    public Guid SecondUserId { get; set; }

    public User SecondUser { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CalendarConnectionConfiguration : IEntityTypeConfiguration<CalendarConnection>
{
    public void Configure(EntityTypeBuilder<CalendarConnection> connection)
    {
        connection.HasKey(value => value.Id);
        connection.HasIndex(value => value.FirstUserId);
        connection.HasIndex(value => value.SecondUserId);
        connection.Property(value => value.Id).HasDefaultValueSql("uuidv7()").ValueGeneratedOnAdd();
        connection.Property(value => value.CreatedAt).HasDefaultValueSql("now()");
        connection
            .HasOne(value => value.FirstUser)
            .WithMany()
            .HasForeignKey(value => value.FirstUserId)
            .OnDelete(DeleteBehavior.Cascade);
        connection
            .HasOne(value => value.SecondUser)
            .WithMany()
            .HasForeignKey(value => value.SecondUserId)
            .OnDelete(DeleteBehavior.Cascade);
        connection.ToTable(table =>
            table.HasCheckConstraint("CK_CalendarConnections_DistinctUsers", "\"FirstUserId\" <> \"SecondUserId\"")
        );
    }
}
