using ApesDb.Api.Tests.Infrastructure.Authentication;
using ApesDb.Api.Tests.Infrastructure.Factories;
using ApesDb.Api.Tests.Infrastructure.Http;
using ApesDb.Api.Tests.TestData;

namespace ApesDb.Api.Tests.Features.Boards.AddBoardEntry;

public sealed class AddBoardEntryTests : IClassFixture<MutableEndpointApiFactory>, IAsyncLifetime
{
    private readonly MutableEndpointApiFactory _factory;

    public AddBoardEntryTests(MutableEndpointApiFactory factory)
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
    public async Task AddingGameIsIdempotent()
    {
        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        var request = new { GameId = BoardEntryTestData.AddableGameId };
        using var firstResponse = await client.PostAsJsonAsync(
            BoardTestSupport.EntriesUrl(BoardTestData.BacklogId),
            request,
            TestContext.Current.CancellationToken
        );
        using var secondResponse = await client.PostAsJsonAsync(
            BoardTestSupport.EntriesUrl(BoardTestData.BacklogId),
            request,
            TestContext.Current.CancellationToken
        );
        var addResponses = await HttpResponseSnapshot.CreateAsync(firstResponse, secondResponse);
        using var getResponse = await client.GetAsync(
            BoardTestSupport.BoardUrl(BoardTestData.BacklogId),
            TestContext.Current.CancellationToken
        );
        var board = await BoardTestSupport.DetailsSnapshotAsync(getResponse);

        await Verify(new { AddResponses = addResponses, BoardResponse = board });
    }

    [Theory]
    [InlineData("missing-game")]
    [InlineData("missing-board")]
    [InlineData("other-owner")]
    public async Task GameCannotBeAddedToInaccessibleOrInvalidTarget(string scenario)
    {
        var boardId = BoardTestData.BacklogId;
        var gameId = BoardEntryTestData.AddableGameId;
        var identity = TestUsers.Owner;
        if (scenario == "missing-game")
        {
            gameId = long.MaxValue;
        }
        else if (scenario == "missing-board")
        {
            boardId = BoardTestData.UnknownId;
        }
        else
        {
            identity = TestUsers.Outsider;
        }

        using var client = ApiTestClient.CreateAuthenticated(_factory, identity);
        using var addResponse = await client.PostAsJsonAsync(
            BoardTestSupport.EntriesUrl(boardId),
            new { GameId = gameId },
            TestContext.Current.CancellationToken
        );
        var add = await HttpResponseSnapshot.CreateAsync(addResponse);
        using var ownerClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var getResponse = await ownerClient.GetAsync(
            BoardTestSupport.BoardUrl(BoardTestData.BacklogId),
            TestContext.Current.CancellationToken
        );
        var board = await BoardTestSupport.DetailsSnapshotAsync(getResponse);

        await Verify(new { AddResponse = add, OwnerBoardResponse = board }).UseParameters(scenario);
    }
}
