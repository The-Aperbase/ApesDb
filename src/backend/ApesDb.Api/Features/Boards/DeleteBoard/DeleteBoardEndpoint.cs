using ApesDb.Api.Features.Notifications.NotificationsStream;
using ApesDb.Common;
using ApesDb.Domain;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Boards.DeleteBoard;

public sealed class DeleteBoardEndpoint : Endpoint<DeleteBoardRequest>
{
    private const string BoardInviteNotificationType = "BoardInvite";

    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly NotificationStreamService _streamService;

    public DeleteBoardEndpoint(
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
        Delete(ApiRoutes.Boards.ById);
        Summary(summary => summary.Summary = "Deletes a board and its entries.");
    }

    public override async Task HandleAsync(DeleteBoardRequest request, CancellationToken ct)
    {
        var userId = User.GetApesDbUserId();
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        var board = await _dbContext.Boards.FindOwnedForUpdateAsync(request.BoardId, userId, ct);
        if (board is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var invitationIds = await _dbContext
            .BoardInvitations.Where(invitation => invitation.BoardId == request.BoardId)
            .Select(invitation => invitation.Id)
            .ToArrayAsync(ct);
        var notifications = await _dbContext
            .Notifications.Where(notification =>
                notification.Type == BoardInviteNotificationType
                && invitationIds.Contains(notification.ResourceId)
                && notification.ResolvedAt == null
            )
            .ToArrayAsync(ct);
        var now = _dateTimeProvider.UtcNow;
        foreach (var notification in notifications)
        {
            notification.IsActionable = false;
            notification.ReadAt = now;
            notification.ResolvedAt = now;
        }

        await _dbContext.BoardEntries.Where(entry => entry.BoardId == request.BoardId).ExecuteDeleteAsync(ct);

        await _dbContext.SaveChangesAsync(ct);
        await _dbContext.Boards.Where(existingBoard => existingBoard.Id == board.Id).ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct);
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
