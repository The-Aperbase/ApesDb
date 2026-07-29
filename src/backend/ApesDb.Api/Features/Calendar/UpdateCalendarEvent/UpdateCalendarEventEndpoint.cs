using ApesDb.Common;
using ApesDb.Domain;
using ApesDb.Domain.Entities.Calendar;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Calendar.UpdateCalendarEvent;

public sealed class UpdateCalendarEventEndpoint : Endpoint<UpdateCalendarEventRequest, CalendarEventResponse>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateCalendarEventEndpoint(ApplicationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public override void Configure()
    {
        Put(ApiRoutes.Calendar.EventById);
        Summary(summary => summary.Summary = "Updates a calendar event, series, or occurrence.");
    }

    public override async Task HandleAsync(UpdateCalendarEventRequest request, CancellationToken ct)
    {
        var userId = User.GetApesDbUserId();
        var root = await _dbContext
            .CalendarEvents.Where(calendarEvent =>
                calendarEvent.Id == request.EventId
                && calendarEvent.OwnerUserId == userId
                && calendarEvent.RecurringEventId == null
            )
            .SingleOrDefaultAsync(ct);
        if (root is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (request.Scope == "occurrence")
        {
            await UpdateOccurrenceAsync(root, request, ct);
            return;
        }

        if (root.Recurrence is not null && request.Scope != "series")
        {
            AddError(request => request.Scope, "Recurring events must be updated as a series or occurrence.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        if (root.Recurrence is null && request.Scope == "series")
        {
            AddError(request => request.Scope, "Only recurring events can be updated as a series.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var startAt = request.Start.ToUniversalTime();
        var endAt = request.End.ToUniversalTime();
        var scheduleChanged =
            root.StartAt != startAt
            || root.EndAt != endAt
            || root.AllDay != request.AllDay
            || root.TimeZoneId != request.TimeZoneId
            || !RecurrencesEqual(root.Recurrence, request.Recurrence);
        var title = request.Title.Trim();

        if (scheduleChanged && root.Recurrence is not null)
        {
            await _dbContext
                .CalendarEvents.Where(calendarEvent => calendarEvent.RecurringEventId == root.Id)
                .ExecuteDeleteAsync(ct);
        }
        else if (root.Title != title && root.Recurrence is not null)
        {
            await _dbContext
                .CalendarEvents.Where(calendarEvent =>
                    calendarEvent.RecurringEventId == root.Id && !calendarEvent.TitleOverridden
                )
                .ExecuteUpdateAsync(setters => setters.SetProperty(calendarEvent => calendarEvent.Title, title), ct);
        }

        root.Title = title;
        root.StartAt = startAt;
        root.EndAt = endAt;
        root.AllDay = request.AllDay;
        root.TimeZoneId = request.TimeZoneId;
        root.Recurrence = request.Recurrence;
        root.RecurrenceUntil = request.Recurrence?.Until?.ToUniversalTime();
        root.UpdatedAt = _dateTimeProvider.OffsetUtcNow;
        await _dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(
            new CalendarEventResponse(
                root.Id,
                root.OwnerUserId,
                root.Title,
                root.StartAt,
                root.EndAt,
                root.AllDay,
                root.TimeZoneId,
                request.Recurrence,
                [],
                root.RecurringEventId,
                root.OriginalStartAt,
                false,
                root.CreatedAt,
                root.UpdatedAt
            ),
            ct
        );
    }

    private async Task UpdateOccurrenceAsync(
        CalendarEvent root,
        UpdateCalendarEventRequest request,
        CancellationToken ct
    )
    {
        if (root.Recurrence is null)
        {
            AddError(request => request.Scope, "Only recurring events can be updated by occurrence.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var originalStart = request.OriginalStart!.Value.ToUniversalTime();
        var exception = await _dbContext
            .CalendarEvents.Where(calendarEvent =>
                calendarEvent.RecurringEventId == root.Id && calendarEvent.OriginalStartAt == originalStart
            )
            .SingleOrDefaultAsync(ct);
        var now = _dateTimeProvider.OffsetUtcNow;
        if (exception is null)
        {
            exception = new CalendarEvent
            {
                Id = Guid.CreateVersion7(),
                OwnerUserId = root.OwnerUserId,
                Title = request.Title.Trim(),
                StartAt = request.Start.ToUniversalTime(),
                EndAt = request.End.ToUniversalTime(),
                AllDay = request.AllDay,
                TimeZoneId = request.TimeZoneId,
                RecurringEventId = root.Id,
                OriginalStartAt = originalStart,
                IsCancelled = false,
                TitleOverridden = request.Title.Trim() != root.Title,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _dbContext.CalendarEvents.Add(exception);
        }
        else
        {
            exception.Title = request.Title.Trim();
            exception.StartAt = request.Start.ToUniversalTime();
            exception.EndAt = request.End.ToUniversalTime();
            exception.AllDay = request.AllDay;
            exception.TimeZoneId = request.TimeZoneId;
            exception.IsCancelled = false;
            exception.TitleOverridden = exception.Title != root.Title;
            exception.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync(ct);
        await Send.OkAsync(
            new CalendarEventResponse(
                exception.Id,
                exception.OwnerUserId,
                exception.Title,
                exception.StartAt,
                exception.EndAt,
                exception.AllDay,
                exception.TimeZoneId,
                null,
                [],
                exception.RecurringEventId,
                exception.OriginalStartAt,
                false,
                exception.CreatedAt,
                exception.UpdatedAt
            ),
            ct
        );
    }

    private static bool RecurrencesEqual(CalendarRecurrenceContract? left, CalendarRecurrenceContract? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Frequency == right.Frequency
            && left.Interval == right.Interval
            && left.Count == right.Count
            && left.Until == right.Until
            && left.ByWeekday.SequenceEqual(right.ByWeekday)
            && left.ByMonthDay.SequenceEqual(right.ByMonthDay)
            && left.ByMonth.SequenceEqual(right.ByMonth)
            && left.WeekStart == right.WeekStart;
    }
}
