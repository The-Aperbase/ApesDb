using ApesDb.Api.Tests.Infrastructure.Authentication;
using ApesDb.Domain.Entities.Boards;
using ApesDb.Domain.Entities.Users;

namespace ApesDb.Api.Tests.TestData;

public static class BoardTestData
{
    public static readonly Guid BacklogId = Guid.Parse("01910000-0000-7000-8000-000000002001");
    public static readonly Guid CompletedId = Guid.Parse("01910000-0000-7000-8000-000000002002");
    public static readonly Guid OutsiderId = Guid.Parse("01910000-0000-7000-8000-000000002003");
    public static readonly Guid UnknownId = Guid.Parse("01910000-0000-7000-8000-000000002099");

    public static Dictionary<Guid, Board> Create(IReadOnlyDictionary<Guid, User> usersById)
    {
        var ownerId = TestUsers.Owner.SeededUserId!.Value;
        var outsiderId = TestUsers.Outsider.SeededUserId!.Value;

        return new Dictionary<Guid, Board>
        {
            [BacklogId] = new()
            {
                Id = BacklogId,
                OwnerUserId = ownerId,
                OwnerUser = usersById[ownerId],
                Name = "Backlog",
                Picture = [0x52, 0x49, 0x46, 0x46, 0x57, 0x45, 0x42, 0x50],
                CreatedAt = new DateTime(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 12, 10, 0, 0, DateTimeKind.Utc),
            },
            [CompletedId] = new()
            {
                Id = CompletedId,
                OwnerUserId = ownerId,
                OwnerUser = usersById[ownerId],
                Name = "Completed",
                CreatedAt = new DateTime(2026, 1, 11, 9, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 13, 10, 0, 0, DateTimeKind.Utc),
            },
            [OutsiderId] = new()
            {
                Id = OutsiderId,
                OwnerUserId = outsiderId,
                OwnerUser = usersById[outsiderId],
                Name = "Oscar's board",
                CreatedAt = new DateTime(2026, 1, 9, 9, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 9, 9, 0, 0, DateTimeKind.Utc),
            },
        };
    }
}
