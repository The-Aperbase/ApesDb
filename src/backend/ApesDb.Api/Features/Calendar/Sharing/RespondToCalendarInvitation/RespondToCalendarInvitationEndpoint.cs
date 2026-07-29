using ApesDb.Api.Features.Notifications.NotificationsStream;
using ApesDb.Common;
using ApesDb.Domain;
using ApesDb.Domain.Entities.Calendar;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Calendar.Sharing.RespondToCalendarInvitation;

public sealed class RespondToCalendarInvitationEndpoint : Endpoint<RespondToCalendarInvitationRequest>
{
    private const string NotificationType = "CalendarInvite";

    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly NotificationStreamService _streamService;

    public RespondToCalendarInvitationEndpoint(
        ApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        NotificationStreamService streamService
    )
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
        _streamService = streamService;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Calendar.RespondToInvite);
        Summary(summary => summary.Summary = "Accepts or declines a calendar invitation.");
    }

    public override async Task HandleAsync(RespondToCalendarInvitationRequest request, CancellationToken ct)
    {
        var userId = User.GetApesDbUserId();
        var invitation = await _dbContext.CalendarInvitations.SingleOrDefaultAsync(
            value => value.Id == request.InviteId && value.InviteeUserId == userId,
            ct
        );
        if (invitation is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (invitation.StatusId == CalendarInvitationStatus.Accepted)
        {
            if (request.Accept)
            {
                await Send.NoContentAsync(ct);
            }
            else
            {
                await Send.ConflictAsync();
            }

            return;
        }

        if (invitation.StatusId != CalendarInvitationStatus.Pending)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var now = _dateTimeProvider.OffsetUtcNow;
        var notificationIdsByUser = new Dictionary<Guid, List<Guid>>();
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        if (request.Accept)
        {
            invitation.StatusId = CalendarInvitationStatus.Accepted;
            await EnsureConnectionAsync(invitation.InviterUserId, userId, now, ct);
            await ResolveReciprocalInvitationsAsync(userId, invitation.InviterUserId, now, notificationIdsByUser, ct);
        }
        else
        {
            invitation.StatusId = CalendarInvitationStatus.Declined;
        }

        invitation.ResolvedAt = now;
        await ResolveNotificationsAsync(userId, [invitation.Id], now, notificationIdsByUser, ct);

        await _dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        PublishResolved(notificationIdsByUser);
        await Send.NoContentAsync(ct);
    }

    private async Task EnsureConnectionAsync(
        Guid firstUserId,
        Guid secondUserId,
        DateTimeOffset now,
        CancellationToken ct
    )
    {
        var exists = await _dbContext.CalendarConnections.AnyAsync(
            connection =>
                (connection.FirstUserId == firstUserId && connection.SecondUserId == secondUserId)
                || (connection.FirstUserId == secondUserId && connection.SecondUserId == firstUserId),
            ct
        );
        if (exists)
        {
            return;
        }

        _dbContext.CalendarConnections.Add(
            new CalendarConnection
            {
                Id = Guid.CreateVersion7(),
                FirstUserId = firstUserId,
                SecondUserId = secondUserId,
                CreatedAt = now,
            }
        );
    }

    private async Task ResolveReciprocalInvitationsAsync(
        Guid inviterId,
        Guid inviteeId,
        DateTimeOffset now,
        Dictionary<Guid, List<Guid>> notificationIdsByUser,
        CancellationToken ct
    )
    {
        var reciprocal = await _dbContext
            .CalendarInvitations.Where(invitation =>
                invitation.InviterUserId == inviterId
                && invitation.InviteeUserId == inviteeId
                && invitation.StatusId == CalendarInvitationStatus.Pending
            )
            .ToArrayAsync(ct);
        foreach (var invitation in reciprocal)
        {
            invitation.StatusId = CalendarInvitationStatus.Accepted;
            invitation.ResolvedAt = now;
        }

        if (reciprocal.Length == 0)
        {
            return;
        }

        await ResolveNotificationsAsync(
            inviteeId,
            reciprocal.Select(invitation => invitation.Id).ToArray(),
            now,
            notificationIdsByUser,
            ct
        );
    }

    private async Task ResolveNotificationsAsync(
        Guid userId,
        Guid[] resourceIds,
        DateTimeOffset now,
        Dictionary<Guid, List<Guid>> notificationIdsByUser,
        CancellationToken ct
    )
    {
        var notifications = await _dbContext
            .Notifications.Where(notification =>
                notification.UserId == userId
                && notification.Type == NotificationType
                && resourceIds.Contains(notification.ResourceId)
                && notification.ResolvedAt == null
            )
            .ToArrayAsync(ct);
        foreach (var notification in notifications)
        {
            notification.IsActionable = false;
            notification.ReadAt = now.UtcDateTime;
            notification.ResolvedAt = now.UtcDateTime;
        }

        if (notifications.Length == 0)
        {
            return;
        }

        notificationIdsByUser[userId] = notifications.Select(notification => notification.Id).ToList();
    }

    private void PublishResolved(Dictionary<Guid, List<Guid>> notificationIdsByUser)
    {
        foreach (var item in notificationIdsByUser)
        {
            foreach (var notificationId in item.Value)
            {
                _streamService.Publish(
                    item.Key,
                    new NotificationStreamEvent(
                        NotificationStreamEventKinds.Resolved,
                        new NotificationResolvedEventData(notificationId)
                    )
                );
            }
        }
    }
}
