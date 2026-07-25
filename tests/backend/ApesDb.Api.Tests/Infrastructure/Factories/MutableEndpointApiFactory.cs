using ApesDb.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using Respawn.Graph;

namespace ApesDb.Api.Tests.Infrastructure.Factories;

public sealed class MutableEndpointApiFactory : ApiTestWebApplicationFactory
{
    private Respawner? _respawner;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        _respawner = await Respawner.CreateAsync(
            dbContext.Database.GetDbConnection(),
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"],
                TablesToIgnore = [new Table("public", "BoardEntryStates")],
            }
        );
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        if (_respawner is null)
        {
            throw new InvalidOperationException("Respawn has not been initialized.");
        }

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        await _respawner.ResetAsync(dbContext.Database.GetDbConnection());
        await SeedAsync(cancellationToken);
    }
}
