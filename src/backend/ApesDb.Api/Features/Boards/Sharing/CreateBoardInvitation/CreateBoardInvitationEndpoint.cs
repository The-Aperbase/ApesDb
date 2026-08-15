using ApesDb.Api.Features.Notifications.GetNotifications;
using ApesDb.Api.Features.Notifications.NotificationsStream;
using ApesDb.Common;
using ApesDb.Domain;
using ApesDb.Domain.Entities.Boards;
using ApesDb.Domain.Entities.Notifications;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ApesDb.Api.Features.Boards.Sharing.CreateBoardInvitation;

public sealed class CreateBoardInvitationEndpoint : Endpoint<CreateBoardInvitationRequest, BoardInvitationResponse>
{
    private const string NotificationType = "BoardInvite";

    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly NotificationStreamService _streamService;

    public CreateBoardInvitationEndpoint(
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
        Post(ApiRoutes.Boards.Invitations);
        Summary(summary => summary.Summary = "Invites someone to collaborate on an owned board.");
    }

    public override async Task HandleAsync(CreateBoardInvitationRequest request, CancellationToken ct)
    {
        var ownerUserId = User.GetApesDbUserId();
        var board = await _dbContext
            .Boards.AsNoTracking()
            .Where(value => value.Id == request.BoardId && value.OwnerUserId == ownerUserId)
            .Select(value => new
            {
                value.Id,
                value.Name,
                Owner = new BoardUserResponse(value.OwnerUserId, value.OwnerUser.Name, value.OwnerUser.PictureUrl),
            })
            .SingleOrDefaultAsync(ct);
        if (board is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var inviteeUserId = await _dbContext
            .Users.AsNoTracking()
            .Where(user => user.Email == normalizedEmail)
            .OrderBy(user => user.Id)
            .Select(user => (Guid?)user.Id)
            .FirstOrDefaultAsync(ct);

        if (inviteeUserId == ownerUserId || await IsCollaboratorAsync(board.Id, inviteeUserId, ct))
        {
            await Send.AcceptedAsync();
            return;
        }

        var existing = await _dbContext.BoardInvitations.SingleOrDefaultAsync(
            invitation =>
                invitation.BoardId == board.Id
                && invitation.InviteeEmail == normalizedEmail
                && invitation.StatusId == BoardInvitationStatus.Pending,
            ct
        );
        if (existing is not null)
        {
            if (existing.InviteeUserId is null && inviteeUserId is not null)
            {
                var notification = CreateNotification(existing, inviteeUserId.Value);
                existing.InviteeUserId = inviteeUserId;
                _dbContext.Notifications.Add(notification);
                await _dbContext.SaveChangesAsync(ct);
                PublishCreated(notification, board.Owner, board.Id, board.Name);
            }

            await Send.AcceptedAsync();
            return;
        }

        var now = _dateTimeProvider.UtcNow;
        var invitation = new BoardInvitation
        {
            Id = Guid.CreateVersion7(),
            BoardId = board.Id,
            InviteeUserId = inviteeUserId,
            InviteeEmail = normalizedEmail,
            StatusId = BoardInvitationStatus.Pending,
            CreatedAt = now,
        };
        _dbContext.BoardInvitations.Add(invitation);

        Notification? createdNotification = null;
        if (inviteeUserId is not null)
        {
            createdNotification = CreateNotification(invitation, inviteeUserId.Value);
            _dbContext.Notifications.Add(createdNotification);
        }

        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            _dbContext.ChangeTracker.Clear();
            await Send.AcceptedAsync();
            return;
        }

        if (createdNotification is not null)
        {
            PublishCreated(createdNotification, board.Owner, board.Id, board.Name);
        }

        var response = new BoardInvitationResponse(
            invitation.Id,
            new BoardInvitationBoardResponse(board.Id, board.Name),
            board.Owner,
            invitation.CreatedAt
        );
        await Send.ResultAsync(TypedResults.Created($"/api/boards/{board.Id}/invitations/{invitation.Id}", response));
    }

    private async Task<bool> IsCollaboratorAsync(Guid boardId, Guid? inviteeUserId, CancellationToken ct)
    {
        if (inviteeUserId is null)
        {
            return false;
        }

        return await _dbContext.BoardCollaborators.AnyAsync(
            collaborator => collaborator.BoardId == boardId && collaborator.UserId == inviteeUserId.Value,
            ct
        );
    }

    private static Notification CreateNotification(BoardInvitation invitation, Guid inviteeUserId)
    {
        return new Notification
        {
            Id = Guid.CreateVersion7(),
            UserId = inviteeUserId,
            Type = NotificationType,
            ResourceId = invitation.Id,
            IsActionable = true,
            CreatedAt = invitation.CreatedAt,
        };
    }

    private void PublishCreated(Notification notification, BoardUserResponse owner, Guid boardId, string boardName)
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
                    new NotificationActorResponse(owner.Id, owner.Name, owner.PictureUrl),
                    new NotificationBoardResponse(boardId, boardName)
                )
            )
        );
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
