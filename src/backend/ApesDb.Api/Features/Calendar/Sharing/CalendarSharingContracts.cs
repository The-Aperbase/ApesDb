namespace ApesDb.Api.Features.Calendar.Sharing;

public sealed record CalendarUserResponse(Guid Id, string Name, string? PictureUrl);

public sealed record CalendarConnectionResponse(Guid Id, CalendarUserResponse User, DateTimeOffset CreatedAt);

public sealed record IncomingCalendarInvitationResponse(
    Guid Id,
    CalendarUserResponse InvitedBy,
    DateTimeOffset CreatedAt
);

public sealed record OutgoingCalendarInvitationResponse(Guid Id, string Email, DateTimeOffset CreatedAt);

public sealed record CalendarSharingResponse(
    CalendarConnectionResponse[] Connections,
    IncomingCalendarInvitationResponse[] IncomingInvitations,
    OutgoingCalendarInvitationResponse[] OutgoingInvitations
);

public sealed record CalendarInvitationResponse(Guid Id, CalendarUserResponse InvitedBy, DateTimeOffset CreatedAt);
