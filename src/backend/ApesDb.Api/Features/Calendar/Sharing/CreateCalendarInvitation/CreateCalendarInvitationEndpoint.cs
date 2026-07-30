using ApesDb.Api.Features.Calendar.Sharing.GetCalendarInvitation;
using ApesDb.Api.Features.Notifications.GetNotifications;
using ApesDb.Api.Features.Notifications.NotificationsStream;
using ApesDb.Common;
using ApesDb.Domain;
using ApesDb.Domain.Entities.Calendar;
using ApesDb.Domain.Entities.Notifications;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Calendar.Sharing.CreateCalendarInvitation;

public sealed class CreateCalendarInvitationEndpoint
    : Endpoint<CreateCalendarInvitationRequest, CalendarInvitationResponse>
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
        var inviteeUserId = await FindInviteeUserIdAsync(normalizedEmail, ct);

        if (await ShouldIgnoreInvitationAsync(inviterId, inviteeUserId, ct))
        {
            await Send.AcceptedAsync();
            return;
        }

        var pendingInvitation = await FindPendingInvitationAsync(inviterId, normalizedEmail, ct);
        if (pendingInvitation is not null)
        {
            if (pendingInvitation.InviteeUserId is null && inviteeUserId is not null)
            {
                await AttachInviteeAndNotifyAsync(pendingInvitation, inviteeUserId.Value, ct);
            }

            await Send.AcceptedAsync();
            return;
        }

        var result = await CreateInvitationAsync(inviterId, inviteeUserId, normalizedEmail, ct);
        await Send.CreatedAtAsync<GetCalendarInvitationEndpoint>(
            new { inviteId = result.Invitation.Id },
            new CalendarInvitationResponse(result.Invitation.Id, result.InvitedBy, result.Invitation.CreatedAt),
            cancellation: ct
        );
    }

    private async Task<Guid?> FindInviteeUserIdAsync(string normalizedEmail, CancellationToken ct)
    {
        return await _dbContext
            .Users.AsNoTracking()
            .Where(user => user.Email == normalizedEmail)
            .Select(user => (Guid?)user.Id) //Guid is a value type, this stops getting an empty guid back
            .FirstOrDefaultAsync(ct);
    }

    private async Task<bool> ShouldIgnoreInvitationAsync(Guid inviterId, Guid? inviteeUserId, CancellationToken ct)
    {
        if (inviteeUserId is null)
        {
            return false;
        }

        if (inviteeUserId.Value == inviterId)
        {
            return true;
        }

        return await _dbContext.CalendarConnections.AnyAsync(
            connection =>
                (connection.FirstUserId == inviterId && connection.SecondUserId == inviteeUserId.Value)
                || (connection.FirstUserId == inviteeUserId.Value && connection.SecondUserId == inviterId),
            ct
        );
    }

    private Task<CalendarInvitation?> FindPendingInvitationAsync(
        Guid inviterId,
        string normalizedEmail,
        CancellationToken ct
    )
    {
        return _dbContext.CalendarInvitations.SingleOrDefaultAsync(
            invitation =>
                invitation.InviterUserId == inviterId
                && invitation.InviteeEmail == normalizedEmail
                && invitation.StatusId == CalendarInvitationStatus.Pending,
            ct
        );
    }

    private async Task AttachInviteeAndNotifyAsync(
        CalendarInvitation invitation,
        Guid inviteeUserId,
        CancellationToken ct
    )
    {
        var invitedBy = await GetInvitedByAsync(invitation.InviterUserId, ct);
        var notification = CreateNotification(invitation, inviteeUserId);
        invitation.InviteeUserId = inviteeUserId;
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(ct);
        PublishNotification(notification, invitedBy);
    }

    private async Task<CreatedInvitationResult> CreateInvitationAsync(
        Guid inviterId,
        Guid? inviteeUserId,
        string normalizedEmail,
        CancellationToken ct
    )
    {
        var now = _dateTimeProvider.OffsetUtcNow;
        var invitation = new CalendarInvitation
        {
            Id = Guid.CreateVersion7(),
            InviterUserId = inviterId,
            InviteeUserId = inviteeUserId,
            InviteeEmail = normalizedEmail,
            StatusId = CalendarInvitationStatus.Pending,
            CreatedAt = now,
        };
        var invitedBy = await GetInvitedByAsync(inviterId, ct);
        _dbContext.CalendarInvitations.Add(invitation);

        Notification? notification = null;
        if (inviteeUserId is not null)
        {
            notification = CreateNotification(invitation, inviteeUserId.Value);
            _dbContext.Notifications.Add(notification);
        }

        await _dbContext.SaveChangesAsync(ct);
        if (notification is not null)
        {
            PublishNotification(notification, invitedBy);
        }

        return new CreatedInvitationResult(invitation, invitedBy);
    }

    private Task<CalendarUserResponse> GetInvitedByAsync(Guid inviterId, CancellationToken ct)
    {
        return _dbContext
            .Users.AsNoTracking()
            .Where(user => user.Id == inviterId)
            .Select(user => new CalendarUserResponse(user.Id, user.Name, user.PictureUrl))
            .SingleAsync(ct);
    }

    private static Notification CreateNotification(CalendarInvitation invitation, Guid inviteeUserId)
    {
        return new Notification
        {
            Id = Guid.CreateVersion7(),
            UserId = inviteeUserId,
            Type = NotificationType,
            ResourceId = invitation.Id,
            IsActionable = true,
            CreatedAt = invitation.CreatedAt.UtcDateTime,
        };
    }

    private void PublishNotification(Notification notification, CalendarUserResponse invitedBy)
    {
        _streamService.Publish(
            notification.UserId,
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
                    new NotificationActorResponse(invitedBy.Id, invitedBy.Name, invitedBy.PictureUrl)
                )
            )
        );
    }

    private sealed record CreatedInvitationResult(CalendarInvitation Invitation, CalendarUserResponse InvitedBy);
}
