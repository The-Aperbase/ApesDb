using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApesDb.Domain.Entities.Boards;

public sealed class BoardEntryState
{
    public int Id { get; set; }

    public required string Name { get; set; }
}

public sealed class BoardEntryStateConfiguration : IEntityTypeConfiguration<BoardEntryState>
{
    public void Configure(EntityTypeBuilder<BoardEntryState> state)
    {
        state.ToTable("BoardEntryStates");
        state.HasKey(value => value.Id);
        state.Property(value => value.Id).ValueGeneratedOnAdd();
        state.Property(value => value.Name).HasMaxLength(16);
        state.HasIndex(value => value.Name).IsUnique();
    }
}
