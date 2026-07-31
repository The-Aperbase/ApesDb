using ApesDb.Api.Tests.Infrastructure.Factories;
using ApesDb.Api.Tests.Infrastructure.Http;

namespace ApesDb.Api.Tests.Features.Health;

public sealed class HealthTests
{
    private readonly SharedGetApiFactory _factory;

    public HealthTests(SharedGetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AnonymousUserCanGetHealth()
    {
        using var client = ApiTestClient.CreateAnonymous(_factory);
        using var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        await Verify(await HttpResponseSnapshot.CreateAsync(response));
    }
}
