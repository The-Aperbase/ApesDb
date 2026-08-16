using ApesDb.Shared.Services.Users;
using ApesDb.Worker.Telemetry;
using TickerQ.Utilities.Base;

namespace ApesDb.Worker.Users;

public sealed class AllowedUserJobs
{
    private readonly IAllowedUserService _allowedUserService;

    public AllowedUserJobs(IAllowedUserService allowedUserService)
    {
        _allowedUserService = allowedUserService;
    }

    [TickerFunction(AllowedUserFunctions.Add, maxConcurrency: 1)]
    public async Task AddAsync(
        TickerFunctionContext<AddAllowedUserRequest> context,
        CancellationToken cancellationToken
    )
    {
        await TickerQTelemetry.RunAsync(
            AllowedUserFunctions.Add,
            context.RetryCount,
            () => _allowedUserService.AddAsync(context.Request.Email, cancellationToken)
        );
    }
}
