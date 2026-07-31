using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace ApesDb.Api.Health;

public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisHealthCheck(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var database = _connectionMultiplexer.GetDatabase();
        await database.PingAsync().WaitAsync(cancellationToken);

        return HealthCheckResult.Healthy();
    }
}
