using ApesDb.Api.Tests.Features.Boards.AddBoardEntry;
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
        using var reorderResponse = await client.PutAsJsonAsync(
            BoardTestSupport.EntryUrl(BoardTestData.BacklogId, BoardEntryTestData.AddableGameId),
            new { State = "todo", Position = 0 },
            TestContext.Current.CancellationToken
        );
        using var moveResponse = await client.PutAsJsonAsync(
            BoardTestSupport.EntryUrl(BoardTestData.BacklogId, BoardEntryTestData.BacklogGameId),
            new { State = "completed", Position = 0 },
            TestContext.Current.CancellationToken
        );
        var mutationResponses = new
        {
            Add = new[]
            {
                await HttpResponseSnapshot.CreateAsync<AddBoardEntryContract>(firstAddResponse),
                await HttpResponseSnapshot.CreateAsync<AddBoardEntryContract>(secondAddResponse),
            },
            Reorder = await HttpResponseSnapshot.CreateAsync(reorderResponse),
            Move = await HttpResponseSnapshot.CreateAsync(moveResponse),
        };
        using var getResponse = await client.GetAsync(
            BoardTestSupport.BoardUrl(BoardTestData.BacklogId),
            TestContext.Current.CancellationToken
        );
        var board = await BoardTestSupport.DetailsSnapshotAsync(getResponse);

        await Verify(new { MutationResponses = mutationResponses, BoardResponse = board });
    }

    [Theory]
    [InlineData("invalid-state")]
    [InlineData("negative-position")]
    [InlineData("out-of-range-position")]
    [InlineData("missing-position")]
    [InlineData("missing-entry")]
    [InlineData("other-owner")]
    public async Task InvalidOrInaccessibleEntryIsNotUpdated(string scenario)
    {
        var gameId = BoardEntryTestData.BacklogGameId;
        var state = "completed";
        var position = 0;
        var identity = TestUsers.Owner;
        object request;
        if (scenario == "invalid-state")
        {
            state = "done";
        }
        else if (scenario == "negative-position")
        {
            position = -1;
        }
        else if (scenario == "out-of-range-position")
        {
            state = "in-progress";
            position = 1;
        }
        else if (scenario == "missing-entry")
        {
            gameId = BoardEntryTestData.AddableGameId;
        }
        else if (scenario == "other-owner")
        {
            identity = TestUsers.Outsider;
        }

        if (scenario == "missing-position")
        {
            request = new { State = state };
        }
        else
        {
            request = new { State = state, Position = position };
        }

        using var client = ApiTestClient.CreateAuthenticated(_factory, identity);
        using var updateResponse = await client.PutAsJsonAsync(
            BoardTestSupport.EntryUrl(BoardTestData.BacklogId, gameId),
            request,
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
