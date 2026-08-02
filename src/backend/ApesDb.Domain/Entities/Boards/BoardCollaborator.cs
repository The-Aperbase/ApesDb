using ApesDb.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.Boards;

public sealed class BoardCollaborator
{
    public Guid BoardId { get; set; }

    public Board Board { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime JoinedAt { get; set; }
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
