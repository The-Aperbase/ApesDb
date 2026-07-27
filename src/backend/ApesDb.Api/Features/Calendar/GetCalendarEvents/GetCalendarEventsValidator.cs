using FastEndpoints;
using FluentValidation;

namespace ApesDb.Api.Features.Calendar.GetCalendarEvents;

public sealed class GetCalendarEventsValidator : Validator<GetCalendarEventsRequest>
{
    public GetCalendarEventsValidator()
    {
        RuleFor(request => request.End).GreaterThan(request => request.Start).WithMessage("End must be after start.");
        RuleFor(request => request)
            .Must(request => request.End - request.Start <= TimeSpan.FromDays(62))
            .WithMessage("Calendar ranges must not exceed 62 days.");
    }
}
