using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.Users;

public sealed class AllowedUser
{
    public Guid Id { get; init; }

    public required string Email { get; init; }

    public DateTime CreatedAt { get; init; }
}

public sealed class AllowedUserConfiguration : IEntityTypeConfiguration<AllowedUser>
{
    public void Configure(EntityTypeBuilder<AllowedUser> allowedUser)
    {
        allowedUser.ToTable(table =>
            table.HasCheckConstraint(
                "CK_AllowedUsers_Email_Normalized",
                """
                "Email" <> '' AND "Email" = btrim("Email") AND "Email" = lower("Email")
                """
            )
        );
        allowedUser.HasKey(value => value.Id);
        allowedUser.HasIndex(value => value.Email).IsUnique();
        allowedUser.Property(value => value.Id).HasDefaultValueSql("uuidv7()").ValueGeneratedOnAdd();
        allowedUser.Property(value => value.Email).HasMaxLength(256);
        allowedUser.Property(value => value.CreatedAt).HasDefaultValueSql("now()");
    }
}
