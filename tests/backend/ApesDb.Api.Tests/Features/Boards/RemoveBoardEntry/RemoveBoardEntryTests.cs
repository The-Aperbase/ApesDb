using ApesDb.Api.Tests.Features.Boards.AddBoardEntry;
using ApesDb.Api.Tests.Infrastructure.Authentication;
using ApesDb.Api.Tests.Infrastructure.Factories;
using ApesDb.Api.Tests.Infrastructure.Http;
using ApesDb.Api.Tests.TestData;

namespace ApesDb.Api.Tests.Features.Boards.RemoveBoardEntry;

public sealed class RemoveBoardEntryTests : IClassFixture<MutableEndpointApiFactory>, IAsyncLifetime
{
    private readonly MutableEndpointApiFactory _factory;

    public RemoveBoardEntryTests(MutableEndpointApiFactory factory)
    {
        _factory = factory;
    }

    public async ValueTask InitializeAsync()
    {
        await _factory.ResetAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task OwnerCanRemoveEntryAndRepeatedRemovalIsNotFound()
    {
        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var firstAddResponse = await client.PostAsJsonAsync(
            BoardTestSupport.EntriesUrl(BoardTestData.BacklogId),
            new { GameId = BoardEntryTestData.AppendPredecessorGameId },
            TestContext.Current.CancellationToken
        );
        using var secondAddResponse = await client.PostAsJsonAsync(
            BoardTestSupport.EntriesUrl(BoardTestData.BacklogId),
            new { GameId = BoardEntryTestData.AddableGameId },
            TestContext.Current.CancellationToken
        );
        var addResponses = new[]
        {
            await HttpResponseSnapshot.CreateAsync<AddBoardEntryContract>(firstAddResponse),
            await HttpResponseSnapshot.CreateAsync<AddBoardEntryContract>(secondAddResponse),
        };
        var url = BoardTestSupport.EntryUrl(BoardTestData.BacklogId, BoardEntryTestData.AppendPredecessorGameId);
        using var firstResponse = await client.DeleteAsync(url, TestContext.Current.CancellationToken);
        using var secondResponse = await client.DeleteAsync(url, TestContext.Current.CancellationToken);
        var removeResponses = await HttpResponseSnapshot.CreateAsync(firstResponse, secondResponse);
        using var getResponse = await client.GetAsync(
            BoardTestSupport.BoardUrl(BoardTestData.BacklogId),
            TestContext.Current.CancellationToken
        );
        var board = await BoardTestSupport.DetailsSnapshotAsync(getResponse);

        await Verify(
            new
            {
                AddResponses = addResponses,
                RemoveResponses = removeResponses,
                BoardResponse = board,
            }
        );
    }

    [Fact]
    public async Task OtherOwnerCannotRemoveEntry()
    {
        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Outsider);
        using var removeResponse = await client.DeleteAsync(
            BoardTestSupport.EntryUrl(BoardTestData.BacklogId, BoardEntryTestData.BacklogGameId),
            TestContext.Current.CancellationToken
        );
        var remove = await HttpResponseSnapshot.CreateAsync(removeResponse);
        using var ownerClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var getResponse = await ownerClient.GetAsync(
            BoardTestSupport.BoardUrl(BoardTestData.BacklogId),
            TestContext.Current.CancellationToken
        );
        var board = await BoardTestSupport.DetailsSnapshotAsync(getResponse);

        await Verify(new { RemoveResponse = remove, OwnerBoardResponse = board });
    }
}
