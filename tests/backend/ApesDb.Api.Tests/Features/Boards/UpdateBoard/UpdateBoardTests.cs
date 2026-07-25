using ApesDb.Api.Tests.Infrastructure.Authentication;
using ApesDb.Api.Tests.Infrastructure.Factories;
using ApesDb.Api.Tests.Infrastructure.Http;
using ApesDb.Api.Tests.TestData;

namespace ApesDb.Api.Tests.Features.Boards.UpdateBoard;

public sealed class UpdateBoardTests : IClassFixture<MutableEndpointApiFactory>, IAsyncLifetime
{
    private readonly MutableEndpointApiFactory _factory;

    public UpdateBoardTests(MutableEndpointApiFactory factory)
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
    public async Task OwnerCanUpdateBoardNameAndPicture()
    {
        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var form = BoardTestSupport.CreateForm("  Next up  ", BoardTestSupport.ValidPng);
        using var updateResponse = await client.PutMultipartAsync(
            BoardTestSupport.BoardUrl(BoardTestData.CompletedId),
            form,
            TestContext.Current.CancellationToken
        );
        var update = await BoardTestSupport.DetailsSnapshotAsync(updateResponse);
        using var getResponse = await client.GetAsync(
            BoardTestSupport.BoardUrl(BoardTestData.CompletedId),
            TestContext.Current.CancellationToken
        );
        var board = await BoardTestSupport.DetailsSnapshotAsync(getResponse);

        await Verify(new { UpdateResponse = update, BoardResponse = board });
    }

    [Fact]
    public async Task OwnerCanRemoveBoardPictureWithoutRenaming()
    {
        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var form = BoardTestSupport.CreateForm(removePicture: true);
        using var updateResponse = await client.PutMultipartAsync(
            BoardTestSupport.BoardUrl(BoardTestData.BacklogId),
            form,
            TestContext.Current.CancellationToken
        );
        var update = await BoardTestSupport.DetailsSnapshotAsync(updateResponse);
        using var getResponse = await client.GetAsync(
            BoardTestSupport.BoardUrl(BoardTestData.BacklogId),
            TestContext.Current.CancellationToken
        );
        var board = await BoardTestSupport.DetailsSnapshotAsync(getResponse);

        await Verify(new { UpdateResponse = update, BoardResponse = board });
    }

    [Fact]
    public async Task InvalidPictureDoesNotChangeBoard()
    {
        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var form = BoardTestSupport.CreateForm("Should not persist", [0x01, 0x02, 0x03]);
        using var updateResponse = await client.PutMultipartAsync(
            BoardTestSupport.BoardUrl(BoardTestData.CompletedId),
            form,
            TestContext.Current.CancellationToken
        );
        var update = await HttpResponseSnapshot.CreateAsync(updateResponse);
        using var getResponse = await client.GetAsync(
            BoardTestSupport.BoardUrl(BoardTestData.CompletedId),
            TestContext.Current.CancellationToken
        );
        var board = await BoardTestSupport.DetailsSnapshotAsync(getResponse);

        await Verify(new { UpdateResponse = update, BoardResponse = board });
    }

    [Theory]
    [InlineData("other-owner")]
    [InlineData("unknown")]
    public async Task InaccessibleBoardCannotBeUpdated(string scenario)
    {
        var boardId = BoardTestData.BacklogId;
        if (scenario == "unknown")
        {
            boardId = BoardTestData.UnknownId;
        }

        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Outsider);
        using var form = BoardTestSupport.CreateForm("Not allowed");
        using var updateResponse = await client.PutMultipartAsync(
            BoardTestSupport.BoardUrl(boardId),
            form,
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
