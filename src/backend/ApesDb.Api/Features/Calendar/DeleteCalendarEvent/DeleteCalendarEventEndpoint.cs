using ApesDb.Common;
using ApesDb.Domain;
using ApesDb.Domain.Entities.Calendar;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Calendar.DeleteCalendarEvent;

public sealed class DeleteCalendarEventEndpoint : Endpoint<DeleteCalendarEventRequest>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteCalendarEventEndpoint(ApplicationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public override void Configure()
    {
        Delete(ApiRoutes.Calendar.EventById);
        Summary(summary => summary.Summary = "Deletes a calendar event, series, or occurrence.");
    }

    public override async Task HandleAsync(DeleteCalendarEventRequest request, CancellationToken ct)
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
            await DeleteOccurrenceAsync(root, request, ct);
            return;
        }

        if (root.RecurrenceJson is not null && request.Scope != "series")
        {
            AddError(request => request.Scope, "Recurring events must be deleted as a series or occurrence.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        if (root.RecurrenceJson is null && request.Scope == "series")
        {
            AddError(request => request.Scope, "Only recurring events can be deleted as a series.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        _dbContext.CalendarEvents.Remove(root);
        await _dbContext.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }

    private async Task DeleteOccurrenceAsync(
        CalendarEvent root,
        DeleteCalendarEventRequest request,
        CancellationToken ct
    )
    {
        if (root.RecurrenceJson is null)
        {
            AddError(request => request.Scope, "Only recurring events can be deleted by occurrence.");
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
                Title = root.Title,
                StartAt = originalStart,
                EndAt = originalStart.Add(root.EndAt - root.StartAt),
                AllDay = root.AllDay,
                TimeZoneId = root.TimeZoneId,
                RecurringEventId = root.Id,
                OriginalStartAt = originalStart,
                IsCancelled = true,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _dbContext.CalendarEvents.Add(exception);
        }
        else
        {
            exception.IsCancelled = true;
            exception.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
