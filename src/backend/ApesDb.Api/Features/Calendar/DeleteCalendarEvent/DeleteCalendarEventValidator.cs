using FastEndpoints;
using FluentValidation;

namespace ApesDb.Api.Features.Calendar.DeleteCalendarEvent;

public sealed class DeleteCalendarEventValidator : Validator<DeleteCalendarEventRequest>
{
    public DeleteCalendarEventValidator()
    {
        RuleFor(request => request.Scope)
            .Must(scope => scope == "event" || scope == "series" || scope == "occurrence")
            .WithMessage("Scope must be event, series, or occurrence.");
        RuleFor(request => request.OriginalStart)
            .NotNull()
            .When(request => request.Scope == "occurrence")
            .WithMessage("Original start is required for occurrence deletion.");
    }
}
