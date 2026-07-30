using ApesDb.Domain;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Calendar.GetCalendarEvents;

public sealed class GetCalendarEventsEndpoint : Endpoint<GetCalendarEventsRequest, CalendarRangeResponse>
{
    private readonly ApplicationDbContext _dbContext;

    public GetCalendarEventsEndpoint(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Calendar.Events);
        Summary(summary => summary.Summary = "Gets visible calendar events for a time range.");
    }

    public override async Task HandleAsync(GetCalendarEventsRequest request, CancellationToken ct)
    {
        var currentUserId = User.GetApesDbUserId();
        var rangeStart = request.Start.ToUniversalTime();
        var rangeEnd = request.End.ToUniversalTime();
        var connectedUserIds = await _dbContext
            .CalendarConnections.AsNoTracking()
            .Where(connection => connection.FirstUserId == currentUserId)
            .Select(connection => connection.SecondUserId)
            .Union(
                _dbContext
                    .CalendarConnections.AsNoTracking()
                    .Where(connection => connection.SecondUserId == currentUserId)
                    .Select(connection => connection.FirstUserId)
            )
            .ToArrayAsync(ct);

        var visibleUserIds = new Guid[connectedUserIds.Length + 1];
        visibleUserIds[0] = currentUserId;
        Array.Copy(connectedUserIds, 0, visibleUserIds, 1, connectedUserIds.Length);

        var users = await _dbContext
            .Users.AsNoTracking()
            .Where(user => visibleUserIds.Contains(user.Id))
            .Select(user => new
            {
                user.Id,
                user.Name,
                user.PictureUrl,
            })
            .ToArrayAsync(ct);

        var resources = new List<CalendarResourceResponse>(users.Length);
        var currentUser = users.Single(user => user.Id == currentUserId);
        resources.Add(new CalendarResourceResponse(currentUser.Id, currentUser.Name, currentUser.PictureUrl, true));
        foreach (
            var user in users
                .Where(user => user.Id != currentUserId)
                .OrderBy(user => user.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(user => user.Id)
        )
        {
            resources.Add(new CalendarResourceResponse(user.Id, user.Name, user.PictureUrl, false));
        }

        var roots = await _dbContext
            .CalendarEvents.AsNoTracking()
            .Where(calendarEvent =>
                visibleUserIds.Contains(calendarEvent.OwnerUserId)
                && calendarEvent.RecurringEventId == null
                && (
                    (
                        calendarEvent.Recurrence == null
                        && calendarEvent.StartAt < rangeEnd
                        && calendarEvent.EndAt > rangeStart
                    )
                    || (
                        calendarEvent.Recurrence != null
                        && calendarEvent.StartAt < rangeEnd
                        && (calendarEvent.RecurrenceUntil == null || calendarEvent.RecurrenceUntil >= rangeStart)
                    )
                )
            )
            .OrderBy(calendarEvent => calendarEvent.StartAt)
            .ThenBy(calendarEvent => calendarEvent.Id)
            .ToArrayAsync(ct);

        var rootIds = roots.Select(calendarEvent => calendarEvent.Id).ToArray();
        var exceptions = await _dbContext
            .CalendarEvents.AsNoTracking()
            .Where(calendarEvent =>
                calendarEvent.RecurringEventId != null && rootIds.Contains(calendarEvent.RecurringEventId.Value)
            )
            .OrderBy(calendarEvent => calendarEvent.OriginalStartAt)
            .ThenBy(calendarEvent => calendarEvent.Id)
            .ToArrayAsync(ct);
        var exceptionsByRoot = exceptions
            .GroupBy(calendarEvent => calendarEvent.RecurringEventId!.Value)
            .ToDictionary(group => group.Key, group => group.ToArray());

        var eventResponses = new List<CalendarEventResponse>();
        foreach (var root in roots)
        {
            var rootExceptions = Array.Empty<Domain.Entities.Calendar.CalendarEvent>();
            if (exceptionsByRoot.TryGetValue(root.Id, out var foundExceptions))
            {
                rootExceptions = foundExceptions;
            }

            var exDates = rootExceptions
                .Where(calendarEvent => calendarEvent.OriginalStartAt is not null)
                .Select(calendarEvent => calendarEvent.OriginalStartAt!.Value)
                .ToArray();
            eventResponses.Add(
                new CalendarEventResponse(
                    root.Id,
                    root.OwnerUserId,
                    root.Title,
                    root.StartAt,
                    root.EndAt,
                    root.AllDay,
                    root.TimeZoneId,
                    root.Recurrence,
                    exDates,
                    root.RecurringEventId,
                    root.OriginalStartAt,
                    root.OwnerUserId != currentUserId,
                    root.CreatedAt,
                    root.UpdatedAt
                )
            );

            foreach (var exception in rootExceptions)
            {
                if (exception.IsCancelled)
                {
                    continue;
                }

                if (exception.StartAt >= rangeEnd || exception.EndAt <= rangeStart)
                {
                    continue;
                }

                eventResponses.Add(
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
                        exception.OwnerUserId != currentUserId,
                        exception.CreatedAt,
                        exception.UpdatedAt
                    )
                );
            }
        }

        await Send.OkAsync(new CalendarRangeResponse(resources.ToArray(), eventResponses.ToArray()), ct);
    }
}
