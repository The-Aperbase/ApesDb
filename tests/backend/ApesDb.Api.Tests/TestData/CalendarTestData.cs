using ApesDb.Api.Tests.Infrastructure.Authentication;
using ApesDb.Domain.Entities.Calendar;
using ApesDb.Domain.Entities.Users;

namespace ApesDb.Api.Tests.TestData;

public static class CalendarTestData
{
    public static readonly Guid OwnerWorkEventId = Guid.Parse("01910000-0000-7000-8000-000000007001");
    public static readonly Guid OwnerRotaEventId = Guid.Parse("01910000-0000-7000-8000-000000007002");
    public static readonly Guid MemberEventId = Guid.Parse("01910000-0000-7000-8000-000000007003");
    public static readonly Guid OutsiderEventId = Guid.Parse("01910000-0000-7000-8000-000000007004");
    public static readonly Guid OwnerMemberConnectionId = Guid.Parse("01910000-0000-7000-8000-000000008001");
    public static readonly Guid PendingInvitationId = Guid.Parse("01910000-0000-7000-8000-000000009001");

    public static object[] Create(IReadOnlyDictionary<Guid, User> usersById)
    {
        var ownerId = TestUsers.Owner.SeededUserId!.Value;
        var memberId = TestUsers.Member.SeededUserId!.Value;
        var inviteeId = TestUsers.Invitee.SeededUserId!.Value;
        var outsiderId = TestUsers.Outsider.SeededUserId!.Value;
        var createdAt = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var recurrence = new CalendarRecurrenceContract
        {
            Frequency = "weekly",
            Interval = 1,
            Until = new DateTimeOffset(2026, 2, 28, 23, 0, 0, TimeSpan.Zero),
            ByWeekday = ["MO", "WE"],
            WeekStart = "MO",
        };

        return
        [
            new CalendarEvent
            {
                Id = OwnerWorkEventId,
                OwnerUserId = ownerId,
                OwnerUser = usersById[ownerId],
                Title = "Office shift",
                StartAt = new DateTimeOffset(2026, 1, 15, 8, 0, 0, TimeSpan.FromHours(1)).ToUniversalTime(),
                EndAt = new DateTimeOffset(2026, 1, 15, 16, 0, 0, TimeSpan.FromHours(1)).ToUniversalTime(),
                TimeZoneId = "Europe/Oslo",
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
            },
            new CalendarEvent
            {
                Id = OwnerRotaEventId,
                OwnerUserId = ownerId,
                OwnerUser = usersById[ownerId],
                Title = "Recurring rota",
                StartAt = new DateTimeOffset(2026, 1, 12, 7, 0, 0, TimeSpan.FromHours(1)).ToUniversalTime(),
                EndAt = new DateTimeOffset(2026, 1, 12, 15, 0, 0, TimeSpan.FromHours(1)).ToUniversalTime(),
                TimeZoneId = "Europe/Oslo",
                Recurrence = recurrence,
                RecurrenceUntil = recurrence.Until,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
            },
            new CalendarEvent
            {
                Id = MemberEventId,
                OwnerUserId = memberId,
                OwnerUser = usersById[memberId],
                Title = "Evening shift",
                StartAt = new DateTimeOffset(2026, 1, 15, 18, 0, 0, TimeSpan.Zero),
                EndAt = new DateTimeOffset(2026, 1, 15, 22, 0, 0, TimeSpan.Zero),
                TimeZoneId = "Europe/London",
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
            },
            new CalendarEvent
            {
                Id = OutsiderEventId,
                OwnerUserId = outsiderId,
                OwnerUser = usersById[outsiderId],
                Title = "Private shift",
                StartAt = new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero),
                EndAt = new DateTimeOffset(2026, 1, 15, 17, 0, 0, TimeSpan.Zero),
                TimeZoneId = "Europe/London",
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
            },
            new CalendarConnection
            {
                Id = OwnerMemberConnectionId,
                FirstUserId = ownerId,
                FirstUser = usersById[ownerId],
                SecondUserId = memberId,
                SecondUser = usersById[memberId],
                CreatedAt = createdAt,
            },
            new CalendarInvitation
            {
                Id = PendingInvitationId,
                InviterUserId = ownerId,
                InviterUser = usersById[ownerId],
                InviteeUserId = inviteeId,
                InviteeUser = usersById[inviteeId],
                InviteeEmail = TestUsers.Invitee.Email,
                StatusId = CalendarInvitationStatus.Pending,
                CreatedAt = createdAt,
            },
        ];
    }
}
