namespace ApesDb.Api.Features.Calendar.Sharing.RespondToCalendarInvitation;

public sealed class RespondToCalendarInvitationRequest
{
    public Guid InviteId { get; init; }

    public bool Accept { get; init; }
}
