using ApesDb.Api.Features.Notifications.NotificationsStream;
using ApesDb.Common;
using ApesDb.Domain;
using ApesDb.Domain.Entities.Calendar;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Calendar.Sharing.CancelCalendarInvitation;

public sealed class CancelCalendarInvitationEndpoint : Endpoint<CancelCalendarInvitationRequest>
{
    private const string NotificationType = "CalendarInvite";

    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly NotificationStreamService _streamService;

    public CancelCalendarInvitationEndpoint(
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
        Delete(ApiRoutes.Calendar.InviteById);
        Summary(summary => summary.Summary = "Cancels a pending outgoing calendar invitation.");
    }

    public override async Task HandleAsync(CancelCalendarInvitationRequest request, CancellationToken ct)
    {
        var userId = User.GetApesDbUserId();
        var invitation = await _dbContext.CalendarInvitations.SingleOrDefaultAsync(
            value =>
                value.Id == request.InviteId
                && value.InviterUserId == userId
                && value.StatusId == CalendarInvitationStatus.Pending,
            ct
        );
        if (invitation is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var now = _dateTimeProvider.OffsetUtcNow;
        invitation.StatusId = CalendarInvitationStatus.Cancelled;
        invitation.ResolvedAt = now;

        var notifications = await _dbContext
            .Notifications.Where(notification =>
                notification.Type == NotificationType
                && notification.ResourceId == invitation.Id
                && notification.ResolvedAt == null
            )
            .ToArrayAsync(ct);
        foreach (var notification in notifications)
        {
            notification.IsActionable = false;
            notification.ReadAt = now.UtcDateTime;
            notification.ResolvedAt = now.UtcDateTime;
        }

        await _dbContext.SaveChangesAsync(ct);
        foreach (var notification in notifications)
        {
            _streamService.Publish(
                notification.UserId,
                new NotificationStreamEvent(
                    NotificationStreamEventKinds.Resolved,
                    new NotificationResolvedEventData(notification.Id)
                )
            );
        }

        await Send.NoContentAsync(ct);
    }
}
