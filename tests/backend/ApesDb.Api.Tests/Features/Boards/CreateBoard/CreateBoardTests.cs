using ApesDb.Api.Tests.Infrastructure.Authentication;
using ApesDb.Api.Tests.Infrastructure.Factories;
using ApesDb.Api.Tests.Infrastructure.Http;

namespace ApesDb.Api.Tests.Features.Boards.CreateBoard;

public sealed class CreateBoardTests : IClassFixture<MutableEndpointApiFactory>, IAsyncLifetime
{
    private readonly MutableEndpointApiFactory _factory;

    public CreateBoardTests(MutableEndpointApiFactory factory)
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
    public async Task OwnerCanCreateBoardWithPicture()
    {
        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var form = BoardTestSupport.CreateForm("  Playing next  ", BoardTestSupport.ValidPng);
        using var createResponse = await client.PostMultipartAsync(
            "/api/boards",
            form,
            TestContext.Current.CancellationToken
        );
        var created = await BoardTestSupport.ReadSummaryAsync(createResponse);
        var create = await BoardTestSupport.SummarySnapshotAsync(createResponse, created.Id);

        using var getResponse = await client.GetAsync(
            BoardTestSupport.BoardUrl(created.Id),
            TestContext.Current.CancellationToken
        );
        var board = await BoardTestSupport.DetailsSnapshotAsync(getResponse, created.Id);

        await Verify(new { CreateResponse = create, BoardResponse = board });
    }

    [Theory]
    [InlineData("whitespace-name")]
    [InlineData("invalid-picture")]
    public async Task InvalidBoardIsRejectedWithoutChangingBoards(string scenario)
    {
        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        MultipartFormDataContent form;
        if (scenario == "whitespace-name")
        {
            form = BoardTestSupport.CreateForm("   ");
        }
        else
        {
            form = BoardTestSupport.CreateForm("Invalid picture", [0x01, 0x02, 0x03]);
        }

        using (form)
        {
            using var createResponse = await client.PostMultipartAsync(
                "/api/boards",
                form,
                TestContext.Current.CancellationToken
            );
            var create = await HttpResponseSnapshot.CreateAsync(createResponse);

            using var listResponse = await client.GetAsync("/api/boards", TestContext.Current.CancellationToken);
            var boards = await BoardTestSupport.ListSnapshotAsync(listResponse);

            await Verify(new { CreateResponse = create, BoardsResponse = boards }).UseParameters(scenario);
        }
    }

    [Fact]
    public async Task AnonymousUserCannotCreateBoard()
    {
        using var client = ApiTestClient.CreateAnonymous(_factory);
        using var form = BoardTestSupport.CreateForm("Anonymous board");
        using var createResponse = await client.PostMultipartAsync(
            "/api/boards",
            form,
            TestContext.Current.CancellationToken
        );
        var create = await HttpResponseSnapshot.CreateAsync(createResponse);

        using var ownerClient = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var listResponse = await ownerClient.GetAsync("/api/boards", TestContext.Current.CancellationToken);
        var boards = await BoardTestSupport.ListSnapshotAsync(listResponse);

        await Verify(new { CreateResponse = create, OwnerBoardsResponse = boards });
    }
}
