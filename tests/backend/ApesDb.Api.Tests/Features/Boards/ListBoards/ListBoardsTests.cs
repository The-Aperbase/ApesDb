using ApesDb.Api.Tests.Infrastructure.Authentication;
using ApesDb.Api.Tests.Infrastructure.Factories;
using ApesDb.Api.Tests.Infrastructure.Http;
using ApesDb.Api.Tests.TestData;

namespace ApesDb.Api.Tests.Features.Boards.ListBoards;

public sealed class ListBoardsTests
{
    private readonly SharedGetApiFactory _factory;

    public ListBoardsTests(SharedGetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OwnerSeesOnlyTheirBoardsWithCounts()
    {
        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var response = await client.GetAsync("/api/boards", TestContext.Current.CancellationToken);

        await Verify(await BoardTestSupport.ListSnapshotAsync(response));
    }

    [Fact]
    public async Task GameFilterReportsWhichBoardsContainTheGame()
    {
        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var response = await client.GetAsync(
            $"/api/boards?gameId={BoardEntryTestData.BacklogGameId}",
            TestContext.Current.CancellationToken
        );

        await Verify(await BoardTestSupport.ListSnapshotAsync(response));
    }

    [Fact]
    public async Task BoardsAreIsolatedByOwner()
    {
        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Outsider);
        using var response = await client.GetAsync("/api/boards", TestContext.Current.CancellationToken);

        await Verify(await BoardTestSupport.ListSnapshotAsync(response));
    }

    [Fact]
    public async Task BoardsCanBePaged()
    {
        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var response = await client.GetAsync(
            "/api/boards?page=2&pageSize=1",
            TestContext.Current.CancellationToken
        );

        await Verify(await BoardTestSupport.ListSnapshotAsync(response));
    }

    [Fact]
    public async Task AnonymousUserCannotListBoards()
    {
        using var client = ApiTestClient.CreateAnonymous(_factory);
        using var response = await client.GetAsync("/api/boards", TestContext.Current.CancellationToken);

        await Verify(await HttpResponseSnapshot.CreateAsync(response));
    }
}
