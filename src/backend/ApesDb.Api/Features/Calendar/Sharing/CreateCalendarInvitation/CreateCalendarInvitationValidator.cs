using ApesDb.Domain.Entities.Calendar;
using FastEndpoints;
using FluentValidation;

namespace ApesDb.Api.Features.Calendar.Sharing.CreateCalendarInvitation;

public sealed class CreateCalendarInvitationValidator : Validator<CreateCalendarInvitationRequest>
{
    public CreateCalendarInvitationValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .MaximumLength(CalendarInvitation.MaximumEmailLength)
            .EmailAddress();
    }
}
