using FluentValidation;

namespace ApesDb.Api.Features.Calendar.UpdateCalendarEvent;

public sealed class UpdateCalendarEventValidator : CalendarEventValidator<UpdateCalendarEventRequest>
{
    public UpdateCalendarEventValidator()
    {
        RuleFor(request => request.Scope)
            .Must(scope => scope == "event" || scope == "series" || scope == "occurrence")
            .WithMessage("Scope must be event, series, or occurrence.");
        RuleFor(request => request.OriginalStart)
            .NotNull()
            .When(request => request.Scope == "occurrence")
            .WithMessage("Original start is required for occurrence changes.");
        RuleFor(request => request.Recurrence)
            .Null()
            .When(request => request.Scope == "occurrence")
            .WithMessage("Occurrence changes cannot define recurrence.");
    }
}
