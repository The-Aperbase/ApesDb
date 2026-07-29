using ApesDb.Domain;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Notifications.GetNotifications;

public sealed class GetNotificationsEndpoint : EndpointWithoutRequest<NotificationsResponse>
{
    private const string CalendarInviteNotificationType = "CalendarInvite";

    private readonly ApplicationDbContext _dbContext;

    public GetNotificationsEndpoint(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Notifications.Get);
        Summary(summary => summary.Summary = "Gets active notifications for the authenticated user.");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetApesDbUserId();
        var rows = await _dbContext
            .Notifications.AsNoTracking()
            .Where(notification => notification.UserId == userId && notification.ResolvedAt == null)
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.Id)
            .Select(notification => new
            {
                notification.Id,
                notification.Type,
                notification.ResourceId,
                notification.CreatedAt,
                notification.ReadAt,
                notification.IsActionable,
            })
            .ToArrayAsync(ct);

        var calendarInvitationIds = rows.Where(row => row.Type == CalendarInviteNotificationType)
            .Select(row => row.ResourceId)
            .ToArray();
        var actorsByInvitationId = await _dbContext
            .CalendarInvitations.AsNoTracking()
            .Where(invitation => calendarInvitationIds.Contains(invitation.Id))
            .Select(invitation => new
            {
                invitation.Id,
                Actor = new NotificationActorResponse(
                    invitation.InviterUserId,
                    invitation.InviterUser.Name,
                    invitation.InviterUser.PictureUrl
                ),
            })
            .ToDictionaryAsync(row => row.Id, row => row.Actor, ct);

        var items = new NotificationResponse[rows.Length];
        var unreadCount = 0;
        var actionableCount = 0;
        var attentionCount = 0;
        for (var index = 0; index < rows.Length; index++)
        {
            var row = rows[index];
            var isUnread = row.ReadAt is null;
            if (isUnread)
            {
                unreadCount++;
            }

            if (row.IsActionable)
            {
                actionableCount++;
            }

            if (isUnread || row.IsActionable)
            {
                attentionCount++;
            }

            NotificationActorResponse? actor = null;
            if (
                row.Type == CalendarInviteNotificationType
                && actorsByInvitationId.TryGetValue(row.ResourceId, out var invitationActor)
            )
            {
                actor = invitationActor;
            }

            items[index] = new NotificationResponse(
                row.Id,
                row.Type,
                row.ResourceId,
                row.CreatedAt,
                row.ReadAt,
                isUnread,
                row.IsActionable,
                actor
            );
        }

        await Send.OkAsync(
            new NotificationsResponse(
                items,
                new NotificationMetadataResponse(rows.Length, unreadCount, actionableCount, attentionCount)
            ),
            ct
        );
    }
}
