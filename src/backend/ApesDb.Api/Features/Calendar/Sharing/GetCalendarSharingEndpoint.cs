using ApesDb.Domain;
using ApesDb.Domain.Entities.Calendar;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Calendar.Sharing;

public sealed class GetCalendarSharingEndpoint : EndpointWithoutRequest<CalendarSharingResponse>
{
    private readonly ApplicationDbContext _dbContext;

    public GetCalendarSharingEndpoint(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Calendar.Sharing);
        Summary(summary => summary.Summary = "Gets calendar connections and pending invitations.");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetApesDbUserId();
        var firstConnections = await _dbContext
            .CalendarConnections.AsNoTracking()
            .Where(connection => connection.FirstUserId == userId)
            .Select(connection => new CalendarConnectionResponse(
                connection.Id,
                new CalendarUserResponse(
                    connection.SecondUserId,
                    connection.SecondUser.Name,
                    connection.SecondUser.PictureUrl
                ),
                connection.CreatedAt
            ))
            .ToArrayAsync(ct);
        var secondConnections = await _dbContext
            .CalendarConnections.AsNoTracking()
            .Where(connection => connection.SecondUserId == userId)
            .Select(connection => new CalendarConnectionResponse(
                connection.Id,
                new CalendarUserResponse(
                    connection.FirstUserId,
                    connection.FirstUser.Name,
                    connection.FirstUser.PictureUrl
                ),
                connection.CreatedAt
            ))
            .ToArrayAsync(ct);
        var connections = firstConnections
            .Concat(secondConnections)
            .OrderBy(connection => connection.User.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(connection => connection.User.Id)
            .ToArray();

        var incoming = await _dbContext
            .CalendarInvitations.AsNoTracking()
            .Where(invitation =>
                invitation.InviteeUserId == userId && invitation.Status == CalendarInvitationStatus.Pending
            )
            .OrderByDescending(invitation => invitation.CreatedAt)
            .Select(invitation => new IncomingCalendarInvitationResponse(
                invitation.Id,
                new CalendarUserResponse(
                    invitation.InviterUserId,
                    invitation.InviterUser.Name,
                    invitation.InviterUser.PictureUrl
                ),
                invitation.CreatedAt
            ))
            .ToArrayAsync(ct);
        var outgoing = await _dbContext
            .CalendarInvitations.AsNoTracking()
            .Where(invitation =>
                invitation.InviterUserId == userId && invitation.Status == CalendarInvitationStatus.Pending
            )
            .OrderByDescending(invitation => invitation.CreatedAt)
            .Select(invitation => new OutgoingCalendarInvitationResponse(
                invitation.Id,
                invitation.InviteeEmail,
                invitation.CreatedAt
            ))
            .ToArrayAsync(ct);

        await Send.OkAsync(new CalendarSharingResponse(connections, incoming, outgoing), ct);
    }
}
