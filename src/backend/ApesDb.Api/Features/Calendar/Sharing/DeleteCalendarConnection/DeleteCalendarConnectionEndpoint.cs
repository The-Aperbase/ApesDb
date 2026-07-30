using ApesDb.Domain;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Calendar.Sharing.DeleteCalendarConnection;

public sealed class DeleteCalendarConnectionEndpoint : Endpoint<DeleteCalendarConnectionRequest>
{
    private readonly ApplicationDbContext _dbContext;

    public DeleteCalendarConnectionEndpoint(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Delete(ApiRoutes.Calendar.ConnectionById);
        Summary(summary => summary.Summary = "Disconnects a shared calendar connection.");
    }

    public override async Task HandleAsync(DeleteCalendarConnectionRequest request, CancellationToken ct)
    {
        var userId = User.GetApesDbUserId();
        var deleted = await _dbContext
            .CalendarConnections.Where(connection =>
                connection.Id == request.ConnectionId
                && (connection.FirstUserId == userId || connection.SecondUserId == userId)
            )
            .ExecuteDeleteAsync(ct);
        if (deleted == 0)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
