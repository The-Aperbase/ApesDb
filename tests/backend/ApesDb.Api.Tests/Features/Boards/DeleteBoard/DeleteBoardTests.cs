using ApesDb.Api.Tests.Infrastructure.Authentication;
using ApesDb.Api.Tests.Infrastructure.Factories;
using ApesDb.Api.Tests.Infrastructure.Http;
using ApesDb.Api.Tests.TestData;

namespace ApesDb.Api.Tests.Features.Boards.DeleteBoard;

public sealed class DeleteBoardTests : IClassFixture<MutableEndpointApiFactory>, IAsyncLifetime
{
    private readonly MutableEndpointApiFactory _factory;

    public DeleteBoardTests(MutableEndpointApiFactory factory)
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
    public async Task OwnerCanDeleteBoardAndItsEntries()
    {
        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var deleteResponse = await client.DeleteAsync(
            BoardTestSupport.BoardUrl(BoardTestData.BacklogId),
            TestContext.Current.CancellationToken
        );
        var delete = await HttpResponseSnapshot.CreateAsync(deleteResponse);
        using var getResponse = await client.GetAsync(
            BoardTestSupport.BoardUrl(BoardTestData.BacklogId),
            TestContext.Current.CancellationToken
        );
        var deletedBoard = await HttpResponseSnapshot.CreateAsync(getResponse);
        using var listResponse = await client.GetAsync("/api/boards", TestContext.Current.CancellationToken);
        var boards = await BoardTestSupport.ListSnapshotAsync(listResponse);

        await Verify(
            new
            {
                DeleteResponse = delete,
                DeletedBoardResponse = deletedBoard,
                BoardsResponse = boards,
            }
        );
    }

    [Theory]
    [InlineData("other-owner")]
    [InlineData("unknown")]
    public async Task InaccessibleBoardCannotBeDeleted(string scenario)
    {
        var boardId = BoardTestData.BacklogId;
        if (scenario == "unknown")
        {
            boardId = BoardTestData.UnknownId;
        }

        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Outsider);
        using var deleteResponse = await client.DeleteAsync(
            BoardTestSupport.BoardUrl(boardId),
            TestContext.Current.CancellationToken
        );
        var delete = await HttpResponseSnapshot.CreateAsync(deleteResponse);
        using var ownerClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var getResponse = await ownerClient.GetAsync(
            BoardTestSupport.BoardUrl(BoardTestData.BacklogId),
            TestContext.Current.CancellationToken
        );
        var board = await BoardTestSupport.DetailsSnapshotAsync(getResponse);

        await Verify(new { DeleteResponse = delete, OwnerBoardResponse = board }).UseParameters(scenario);
    }
}
