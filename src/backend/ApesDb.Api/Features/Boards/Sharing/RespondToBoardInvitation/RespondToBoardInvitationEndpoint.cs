using ApesDb.Api.Features.Notifications.NotificationsStream;
using ApesDb.Common;
using ApesDb.Domain;
using ApesDb.Domain.Entities.Boards;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Boards.Sharing.RespondToBoardInvitation;

public sealed class RespondToBoardInvitationEndpoint : Endpoint<RespondToBoardInvitationRequest>
{
    private const string NotificationType = "BoardInvite";

    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly NotificationStreamService _streamService;

    public RespondToBoardInvitationEndpoint(
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
        Post(ApiRoutes.Boards.RespondToInvitation);
        Summary(summary => summary.Summary = "Accepts or declines a board collaboration invitation.");
    }

    public override async Task HandleAsync(RespondToBoardInvitationRequest request, CancellationToken ct)
    {
        var userId = User.GetApesDbUserId();
        var invitation = await _dbContext.BoardInvitations.SingleOrDefaultAsync(
            value =>
                value.Id == request.InvitationId
                && value.BoardId == request.BoardId
                && value.InviteeUserId == userId,
            ct
        );
        if (invitation is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (invitation.StatusId == BoardInvitationStatus.Accepted)
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

        if (invitation.StatusId != BoardInvitationStatus.Pending)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var now = _dateTimeProvider.UtcNow;
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        if (request.Accept)
        {
            invitation.StatusId = BoardInvitationStatus.Accepted;
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "public"."BoardCollaborators" ("BoardId", "UserId", "JoinedAt")
                VALUES ({request.BoardId}, {userId}, {now})
                ON CONFLICT ("BoardId", "UserId") DO NOTHING
                """,
                ct
            );
        }
        else
        {
            invitation.StatusId = BoardInvitationStatus.Declined;
        }

        invitation.ResolvedAt = now;
        var notifications = await _dbContext
            .Notifications.Where(notification =>
                notification.UserId == userId
                && notification.Type == NotificationType
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
