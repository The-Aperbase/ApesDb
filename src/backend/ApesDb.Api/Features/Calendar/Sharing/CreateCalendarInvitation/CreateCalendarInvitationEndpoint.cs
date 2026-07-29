using ApesDb.Api.Features.Notifications.GetNotifications;
using ApesDb.Api.Features.Notifications.NotificationsStream;
using ApesDb.Common;
using ApesDb.Domain;
using ApesDb.Domain.Entities.Calendar;
using ApesDb.Domain.Entities.Notifications;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Calendar.Sharing.CreateCalendarInvitation;

public sealed class CreateCalendarInvitationEndpoint : Endpoint<CreateCalendarInvitationRequest>
{
    private const string NotificationType = "CalendarInvite";

    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly NotificationStreamService _streamService;

    public CreateCalendarInvitationEndpoint(
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
        Post(ApiRoutes.Calendar.Invites);
        Summary(summary => summary.Summary = "Invites a user to mutually share calendars.");
    }

    public override async Task HandleAsync(CreateCalendarInvitationRequest request, CancellationToken ct)
    {
        var inviterId = User.GetApesDbUserId();
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var targetIds = await _dbContext
            .Users.AsNoTracking()
            .Where(user => user.Email.ToLower() == normalizedEmail)
            .OrderBy(user => user.Id)
            .Select(user => user.Id)
            .Take(2)
            .ToArrayAsync(ct);
        Guid? targetId = null;
        if (targetIds.Length == 1)
        {
            targetId = targetIds[0];
        }

        if (targetId == inviterId)
        {
            await Send.AcceptedAsync();
            return;
        }

        if (targetId is not null)
        {
            var alreadyConnected = await _dbContext.CalendarConnections.AnyAsync(
                connection =>
                    (connection.FirstUserId == inviterId && connection.SecondUserId == targetId.Value)
                    || (connection.FirstUserId == targetId.Value && connection.SecondUserId == inviterId),
                ct
            );
            if (alreadyConnected)
            {
                await Send.AcceptedAsync();
                return;
            }
        }

        var pending = await _dbContext.CalendarInvitations.SingleOrDefaultAsync(
            invitation =>
                invitation.InviterUserId == inviterId
                && invitation.InviteeEmail == normalizedEmail
                && invitation.StatusId == CalendarInvitationStatus.Pending,
            ct
        );
        if (pending is not null)
        {
            if (pending.InviteeUserId is null && targetId is not null)
            {
                pending.InviteeUserId = targetId;
                await AddNotificationAndSaveAsync(pending, targetId.Value, ct);
            }

            await Send.AcceptedAsync();
            return;
        }

        var now = _dateTimeProvider.OffsetUtcNow;
        var invitation = new CalendarInvitation
        {
            Id = Guid.CreateVersion7(),
            InviterUserId = inviterId,
            InviteeUserId = targetId,
            InviteeEmail = normalizedEmail,
            StatusId = CalendarInvitationStatus.Pending,
            CreatedAt = now,
        };
        _dbContext.CalendarInvitations.Add(invitation);

        if (targetId is null)
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        else
        {
            await AddNotificationAndSaveAsync(invitation, targetId.Value, ct);
        }

        await Send.AcceptedAsync();
    }

    private async Task AddNotificationAndSaveAsync(CalendarInvitation invitation, Guid targetId, CancellationToken ct)
    {
        var createdAt = invitation.CreatedAt.UtcDateTime;
        var actor = await _dbContext
            .Users.AsNoTracking()
            .Where(user => user.Id == invitation.InviterUserId)
            .Select(user => new NotificationActorResponse(user.Id, user.Name, user.PictureUrl))
            .SingleAsync(ct);
        var notification = new Notification
        {
            Id = Guid.CreateVersion7(),
            UserId = targetId,
            Type = NotificationType,
            ResourceId = invitation.Id,
            IsActionable = true,
            CreatedAt = createdAt,
        };
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(ct);
        _streamService.Publish(
            targetId,
            new NotificationStreamEvent(
                NotificationStreamEventKinds.Created,
                new NotificationResponse(
                    notification.Id,
                    notification.Type,
                    notification.ResourceId,
                    notification.CreatedAt,
                    null,
                    true,
                    true,
                    actor
                )
            )
        );
    }
}
