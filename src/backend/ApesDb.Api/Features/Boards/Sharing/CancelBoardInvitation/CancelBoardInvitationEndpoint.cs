using ApesDb.Api.Features.Notifications.NotificationsStream;
using ApesDb.Common;
using ApesDb.Domain;
using ApesDb.Domain.Entities.Boards;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Boards.Sharing.CancelBoardInvitation;

public sealed class CancelBoardInvitationEndpoint : Endpoint<CancelBoardInvitationRequest>
{
    private const string NotificationType = "BoardInvite";

    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly NotificationStreamService _streamService;

    public CancelBoardInvitationEndpoint(
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
        Delete(ApiRoutes.Boards.InvitationById);
        Summary(summary => summary.Summary = "Cancels a pending invitation for an owned board.");
    }

    public override async Task HandleAsync(CancelBoardInvitationRequest request, CancellationToken ct)
    {
        var ownerUserId = User.GetApesDbUserId();
        var invitation = await _dbContext.BoardInvitations.SingleOrDefaultAsync(
            value =>
                value.Id == request.InvitationId
                && value.BoardId == request.BoardId
                && value.StatusId == BoardInvitationStatus.Pending
                && value.Board.OwnerUserId == ownerUserId,
            ct
        );
        if (invitation is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var now = _dateTimeProvider.UtcNow;
        invitation.StatusId = BoardInvitationStatus.Cancelled;
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
            notification.ReadAt = now;
            notification.ResolvedAt = now;
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
