using ApesDb.Api.Tests.Infrastructure.Authentication;
using ApesDb.Api.Tests.Infrastructure.Factories;
using ApesDb.Api.Tests.Infrastructure.Http;
using ApesDb.Api.Tests.TestData;

namespace ApesDb.Api.Tests.Features.Boards.GetBoard;

public sealed class GetBoardTests
{
    private readonly SharedGetApiFactory _factory;

    public GetBoardTests(SharedGetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OwnerCanGetBoardWithOrderedGameDetails()
    {
        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Owner);
        using var response = await client.GetAsync(
            BoardTestSupport.BoardUrl(BoardTestData.BacklogId),
            TestContext.Current.CancellationToken
        );

        await Verify(await BoardTestSupport.DetailsSnapshotAsync(response));
    }

    [Fact]
    public async Task CollaboratorCanGetBoardWithOrderedGameDetails()
    {
        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Member);
        using var response = await client.GetAsync(
            BoardTestSupport.BoardUrl(BoardTestData.BacklogId),
            TestContext.Current.CancellationToken
        );

        await Verify(await BoardTestSupport.DetailsSnapshotAsync(response));
    }

    [Theory]
    [InlineData("outsider")]
    [InlineData("unknown")]
    public async Task UserGetsNotFoundForInaccessibleBoard(string scenario)
    {
        var boardId = BoardTestData.BacklogId;
        if (scenario == "unknown")
        {
            boardId = BoardTestData.UnknownId;
        }

        using var client = ApiTestClient.CreateAuthenticated(_factory, TestUsers.Outsider);
        using var response = await client.GetAsync(
            BoardTestSupport.BoardUrl(boardId),
            TestContext.Current.CancellationToken
        );

        await Verify(await HttpResponseSnapshot.CreateAsync(response)).UseParameters(scenario);
    }

    [Fact]
    public async Task AnonymousUserCannotGetBoard()
    {
        using var client = ApiTestClient.CreateAnonymous(_factory);
        using var response = await client.GetAsync(
            BoardTestSupport.BoardUrl(BoardTestData.BacklogId),
            TestContext.Current.CancellationToken
        );

        await Verify(await HttpResponseSnapshot.CreateAsync(response));
    }
}
