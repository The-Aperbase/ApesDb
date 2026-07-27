using ApesDb.Domain;
using ApesDb.Domain.Entities.Calendar;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Calendar.Sharing.GetCalendarInvitation;

public sealed class GetCalendarInvitationEndpoint : Endpoint<GetCalendarInvitationRequest, CalendarInvitationResponse>
{
    private readonly ApplicationDbContext _dbContext;

    public GetCalendarInvitationEndpoint(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Calendar.InviteById);
        Summary(summary => summary.Summary = "Gets a pending calendar invitation.");
    }

    public override async Task HandleAsync(GetCalendarInvitationRequest request, CancellationToken ct)
    {
        var userId = User.GetApesDbUserId();
        var response = await _dbContext
            .CalendarInvitations.AsNoTracking()
            .Where(invitation =>
                invitation.Id == request.InviteId
                && invitation.InviteeUserId == userId
                && invitation.Status == CalendarInvitationStatus.Pending
            )
            .Select(invitation => new CalendarInvitationResponse(
                invitation.Id,
                new CalendarUserResponse(
                    invitation.InviterUserId,
                    invitation.InviterUser.Name,
                    invitation.InviterUser.PictureUrl
                ),
                invitation.CreatedAt
            ))
            .SingleOrDefaultAsync(ct);
        if (response is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(response, ct);
    }
}
