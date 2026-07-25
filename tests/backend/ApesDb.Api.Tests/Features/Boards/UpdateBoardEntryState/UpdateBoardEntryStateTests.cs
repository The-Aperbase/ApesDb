using ApesDb.Api.Tests.Infrastructure.Authentication;
using ApesDb.Api.Tests.Infrastructure.Factories;
using ApesDb.Api.Tests.Infrastructure.Http;
using ApesDb.Api.Tests.TestData;

namespace ApesDb.Api.Tests.Features.Boards.UpdateBoardEntryState;

public sealed class UpdateBoardEntryStateTests : IClassFixture<MutableEndpointApiFactory>, IAsyncLifetime
{
    private readonly MutableEndpointApiFactory _factory;

    public UpdateBoardEntryStateTests(MutableEndpointApiFactory factory)
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
    public async Task OwnerCanUpdateEntryState()
    {
        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var updateResponse = await client.PutAsJsonAsync(
            BoardTestSupport.EntryUrl(BoardTestData.BacklogId, BoardEntryTestData.BacklogGameId),
            new { State = "dnf" },
            TestContext.Current.CancellationToken
        );
        var update = await HttpResponseSnapshot.CreateAsync(updateResponse);
        using var getResponse = await client.GetAsync(
            BoardTestSupport.BoardUrl(BoardTestData.BacklogId),
            TestContext.Current.CancellationToken
        );
        var board = await BoardTestSupport.DetailsSnapshotAsync(getResponse);

        await Verify(new { UpdateResponse = update, BoardResponse = board });
    }

    [Theory]
    [InlineData("invalid-state")]
    [InlineData("missing-entry")]
    [InlineData("other-owner")]
    public async Task InvalidOrInaccessibleEntryIsNotUpdated(string scenario)
    {
        var gameId = BoardEntryTestData.BacklogGameId;
        var state = "completed";
        var identity = TestUsers.Owner;
        if (scenario == "invalid-state")
        {
            state = "done";
        }
        else if (scenario == "missing-entry")
        {
            gameId = BoardEntryTestData.AddableGameId;
        }
        else
        {
            identity = TestUsers.Outsider;
        }

        using var client = ApiTestClient.CreateAuthenticated(_factory, identity);
        using var updateResponse = await client.PutAsJsonAsync(
            BoardTestSupport.EntryUrl(BoardTestData.BacklogId, gameId),
            new { State = state },
            TestContext.Current.CancellationToken
        );
        var update = await HttpResponseSnapshot.CreateAsync(updateResponse);
        using var ownerClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var getResponse = await ownerClient.GetAsync(
            BoardTestSupport.BoardUrl(BoardTestData.BacklogId),
            TestContext.Current.CancellationToken
        );
        var board = await BoardTestSupport.DetailsSnapshotAsync(getResponse);

        await Verify(new { UpdateResponse = update, OwnerBoardResponse = board }).UseParameters(scenario);
    }
}
