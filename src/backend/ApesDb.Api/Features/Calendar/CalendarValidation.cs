using System.Text.RegularExpressions;
using ApesDb.Domain.Entities.Calendar;
using FastEndpoints;
using FluentValidation;

namespace ApesDb.Api.Features.Calendar;

public interface ICalendarEventMutationRequest
{
    string Title { get; }

    DateTimeOffset Start { get; }

    DateTimeOffset End { get; }

    bool AllDay { get; }

    string TimeZoneId { get; }

    CalendarRecurrenceContract? Recurrence { get; }
}

public abstract partial class CalendarEventValidator<TRequest> : Validator<TRequest>
    where TRequest : ICalendarEventMutationRequest
{
    protected CalendarEventValidator()
    {
        RuleFor(request => request.Title)
            .Must(title => !string.IsNullOrWhiteSpace(title))
            .WithMessage("Title must not be empty.")
            .MaximumLength(CalendarEvent.MaximumTitleLength);
        RuleFor(request => request.End).GreaterThan(request => request.Start).WithMessage("End must be after start.");
        RuleFor(request => request.TimeZoneId)
            .Must(IsTimeZone)
            .WithMessage("Time zone must be a valid IANA time zone.")
            .MaximumLength(CalendarEvent.MaximumTimeZoneIdLength);
        RuleFor(request => request)
            .Must(HasAlignedAllDayRange)
            .WithMessage("All-day event boundaries must be midnight in the selected time zone.");
        RuleFor(request => request.Recurrence).Must(IsValidRecurrence).WithMessage("Recurrence rule is invalid.");
        RuleFor(request => request)
            .Must(request => request.Recurrence?.Until is null || request.Recurrence.Until >= request.Start)
            .WithMessage("Recurrence end must not be before the event start.");
    }

    private static bool IsTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    private static bool HasAlignedAllDayRange(TRequest request)
    {
        if (!request.AllDay)
        {
            return true;
        }

        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId);
            var start = TimeZoneInfo.ConvertTime(request.Start, timeZone);
            var end = TimeZoneInfo.ConvertTime(request.End, timeZone);
            return start.TimeOfDay == TimeSpan.Zero && end.TimeOfDay == TimeSpan.Zero;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    private static bool IsValidRecurrence(CalendarRecurrenceContract? recurrence)
    {
        if (recurrence is null)
        {
            return true;
        }

        if (
            recurrence.Frequency != "daily"
            && recurrence.Frequency != "weekly"
            && recurrence.Frequency != "monthly"
            && recurrence.Frequency != "yearly"
        )
        {
            return false;
        }

        if (recurrence.Interval < 1 || recurrence.Interval > 365)
        {
            return false;
        }

        if (recurrence.Count is < 1 or > 1000)
        {
            return false;
        }

        if (recurrence.Count is not null && recurrence.Until is not null)
        {
            return false;
        }

        foreach (var weekday in recurrence.ByWeekday)
        {
            if (!WeekdayRegex().IsMatch(weekday))
            {
                return false;
            }
        }

        foreach (var monthDay in recurrence.ByMonthDay)
        {
            if (monthDay == 0 || monthDay < -31 || monthDay > 31)
            {
                return false;
            }
        }

        foreach (var month in recurrence.ByMonth)
        {
            if (month < 1 || month > 12)
            {
                return false;
            }
        }

        if (recurrence.WeekStart is not null && !PlainWeekdayRegex().IsMatch(recurrence.WeekStart))
        {
            return false;
        }

        return true;
    }

    [GeneratedRegex("^([+-]?[1-5])?(MO|TU|WE|TH|FR|SA|SU)$", RegexOptions.CultureInvariant)]
    private static partial Regex WeekdayRegex();

    [GeneratedRegex("^(MO|TU|WE|TH|FR|SA|SU)$", RegexOptions.CultureInvariant)]
    private static partial Regex PlainWeekdayRegex();
}
