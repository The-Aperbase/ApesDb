using ApesDb.Api.Tests.Infrastructure.Authentication;
using ApesDb.Api.Tests.Infrastructure.Time;
using ApesDb.Domain.Entities.Notifications;
using ApesDb.Domain.Entities.Users;

namespace ApesDb.Api.Tests.TestData;

internal static class NotificationTestData
{
    public static Notification[] Create(IReadOnlyDictionary<Guid, User> usersById)
    {
        var inviteeId = TestUsers.Invitee.SeededUserId!.Value;
        var outsiderId = TestUsers.Outsider.SeededUserId!.Value;

        return
        [
            new Notification
            {
                Id = Guid.Parse("01910000-0000-7000-8000-000000005001"),
                UserId = inviteeId,
                User = usersById[inviteeId],
                Type = "CatalogImportCompleted",
                ResourceId = Guid.Parse("01910000-0000-7000-8000-000000006001"),
                CreatedAt = TestClock.UtcNow,
            },
            new Notification
            {
                Id = Guid.Parse("01910000-0000-7000-8000-000000005002"),
                UserId = outsiderId,
                User = usersById[outsiderId],
                Type = "CatalogImportCompleted",
                ResourceId = Guid.Parse("01910000-0000-7000-8000-000000006002"),
                CreatedAt = TestClock.UtcNow,
            },
            new Notification
            {
                Id = Guid.Parse("01910000-0000-7000-8000-000000005003"),
                UserId = inviteeId,
                User = usersById[inviteeId],
                Type = "CatalogImportFailed",
                ResourceId = Guid.Parse("01910000-0000-7000-8000-000000006003"),
                IsActionable = true,
                CreatedAt = TestClock.UtcNow.AddMinutes(-1),
                ResolvedAt = TestClock.UtcNow,
            },
        ];
    }
}
