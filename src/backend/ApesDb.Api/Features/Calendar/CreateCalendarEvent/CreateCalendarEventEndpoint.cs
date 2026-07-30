using ApesDb.Common;
using ApesDb.Domain;
using ApesDb.Domain.Entities.Calendar;
using FastEndpoints;

namespace ApesDb.Api.Features.Calendar.CreateCalendarEvent;

public sealed class CreateCalendarEventEndpoint : Endpoint<CreateCalendarEventRequest, CalendarEventResponse>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateCalendarEventEndpoint(ApplicationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Calendar.Events);
        Summary(summary => summary.Summary = "Creates a calendar event.");
    }

    public override async Task HandleAsync(CreateCalendarEventRequest request, CancellationToken ct)
    {
        var now = _dateTimeProvider.OffsetUtcNow;
        var calendarEvent = new CalendarEvent
        {
            Id = Guid.CreateVersion7(),
            OwnerUserId = User.GetApesDbUserId(),
            Title = request.Title.Trim(),
            StartAt = request.Start.ToUniversalTime(),
            EndAt = request.End.ToUniversalTime(),
            AllDay = request.AllDay,
            TimeZoneId = request.TimeZoneId,
            Recurrence = request.Recurrence,
            RecurrenceUntil = request.Recurrence?.Until?.ToUniversalTime(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        _dbContext.CalendarEvents.Add(calendarEvent);
        await _dbContext.SaveChangesAsync(ct);

        var response = new CalendarEventResponse(
            calendarEvent.Id,
            calendarEvent.OwnerUserId,
            calendarEvent.Title,
            calendarEvent.StartAt,
            calendarEvent.EndAt,
            calendarEvent.AllDay,
            calendarEvent.TimeZoneId,
            request.Recurrence,
            [],
            calendarEvent.RecurringEventId,
            calendarEvent.OriginalStartAt,
            false,
            calendarEvent.CreatedAt,
            calendarEvent.UpdatedAt
        );
        await Send.ResponseAsync(response, StatusCodes.Status201Created, ct);
    }
}
