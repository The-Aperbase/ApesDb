using System.Security.Claims;
using System.Text.Json;
using ApesDb.Api.Features.GamesLists;
using ApesDb.Api.Features.GamesLists.AddGamesListEntry;
using ApesDb.Api.Features.GamesLists.CreateGamesList;
using ApesDb.Api.Features.GamesLists.DeleteGamesList;
using ApesDb.Api.Features.GamesLists.GetGamesList;
using ApesDb.Api.Features.GamesLists.ListGamesLists;
using ApesDb.Api.Features.GamesLists.RemoveGamesListEntry;
using ApesDb.Api.Features.GamesLists.UpdateGamesList;
using ApesDb.Api.Features.GamesLists.UpdateGamesListEntryState;
using ApesDb.Domain;
using ApesDb.Domain.Entities.Games;
using ApesDb.Domain.Entities.GamesLists;
using ApesDb.Domain.Entities.Teams;
using ApesDb.Domain.Entities.Users;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using Xunit;

namespace ApesDb.GamesLists.IntegrationTests;

public sealed class GamesListEndpointTests : IClassFixture<GamesListDatabaseFixture>
{
    private static readonly DateTime Now = new(2026, 7, 17, 20, 0, 0, DateTimeKind.Utc);

    private readonly GamesListDatabaseFixture _database;
    private readonly FixedDateTimeProvider _dateTimeProvider = new(Now);

    public GamesListEndpointTests(GamesListDatabaseFixture database)
    {
        _database = database;
    }

    [Fact]
    public async Task CreateGamesList_CreatesListWithNormalizedPicture()
    {
        await _database.ResetAsync();
        var owner = CreateUser("owner@example.com", "Owner");
        var team = CreateTeam(owner.Id);
        await SeedAsync(owner, team, CreateAcceptedMembership(team.Id, owner.Id));
        var png = CreatePng(400, 200);
        var formFile = new FormFile(new MemoryStream(png), 0, png.Length, "Picture", "list.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png",
        };

        await using var dbContext = _database.CreateDbContext();
        var context = CreateHttpContext(owner.Id);
        var endpoint = Factory.Create<CreateGamesListEndpoint>(
            context,
            dbContext,
            _dateTimeProvider,
            new GamesListPictureProcessor()
        );
        await endpoint.HandleAsync(
            new CreateGamesListRequest { TeamId = team.Id, Name = "  Backlog  ", Picture = formFile },
            default
        );

        Assert.Equal(StatusCodes.Status201Created, context.Response.StatusCode);
        var list = await dbContext.GamesLists.SingleAsync();
        Assert.Equal("Backlog", list.Name);
        Assert.Equal(team.Id, list.TeamId);
        Assert.Equal(Now, list.CreatedAt);
        Assert.Equal(Now, list.UpdatedAt);
        Assert.NotNull(list.Picture);
        using var imageStream = new MemoryStream(list.Picture);
        using var codec = SKCodec.Create(imageStream);
        Assert.NotNull(codec);
        Assert.Equal(SKEncodedImageFormat.Webp, codec.EncodedFormat);
        using var decoded = SKBitmap.Decode(codec);
        Assert.NotNull(decoded);
        Assert.Equal(256, decoded.Width);
        Assert.Equal(256, decoded.Height);
        var response = await ReadResponseAsync<GamesListSummaryResponse>(context);
        Assert.Equal(list.Id, response.Id);
        Assert.Equal("Backlog", response.Name);
        Assert.Equal(0, response.GameCount);
        Assert.NotNull(response.Picture);
    }

    [Fact]
    public async Task CreateGamesList_ReturnsNotFoundForUserWithoutAcceptedMembership()
    {
        await _database.ResetAsync();
        var owner = CreateUser("owner@example.com", "Owner");
        var outsider = CreateUser("outsider@example.com", "Outsider");
        var team = CreateTeam(owner.Id);
        await SeedAsync(owner, outsider, team, CreateAcceptedMembership(team.Id, owner.Id));

        var status = await CreateListStatusAsync(outsider.Id, team.Id);

        Assert.Equal(StatusCodes.Status404NotFound, status);
        await using var dbContext = _database.CreateDbContext();
        Assert.Empty(await dbContext.GamesLists.ToArrayAsync());
    }

    [Fact]
    public async Task ListGamesLists_ReturnsCountsAndContainsGameFlags()
    {
        await _database.ResetAsync();
        var owner = CreateUser("owner@example.com", "Owner");
        var outsider = CreateUser("outsider@example.com", "Outsider");
        var team = CreateTeam(owner.Id);
        var wishlist = CreateGamesList(team.Id, "Wishlist");
        var backlog = CreateGamesList(team.Id, "Backlog");
        await SeedAsync(
            owner,
            outsider,
            team,
            CreateAcceptedMembership(team.Id, owner.Id),
            CreateGame(1001, "Game One"),
            CreateGame(1002, "Game Two"),
            CreateGame(1003, "Game Three"),
            wishlist,
            backlog,
            CreateEntry(wishlist.Id, 1001),
            CreateEntry(wishlist.Id, 1002),
            CreateEntry(backlog.Id, 1003)
        );

        var summaries = await ListGamesListsAsync(owner.Id, team.Id, null);

        Assert.Equal(2, summaries.Length);
        Assert.Equal(backlog.Id, summaries[0].Id);
        Assert.Equal(1, summaries[0].GameCount);
        Assert.False(summaries[0].ContainsGame);
        Assert.Equal(wishlist.Id, summaries[1].Id);
        Assert.Equal(2, summaries[1].GameCount);
        Assert.False(summaries[1].ContainsGame);

        var withGame = await ListGamesListsAsync(owner.Id, team.Id, 1003);

        Assert.True(withGame[0].ContainsGame);
        Assert.False(withGame[1].ContainsGame);

        var outsiderSummaries = await ListGamesListsAsync(outsider.Id, team.Id, null);

        Assert.Empty(outsiderSummaries);
    }

    [Fact]
    public async Task GetGamesList_ReturnsEntriesInAddedOrderWithGameDetails()
    {
        await _database.ResetAsync();
        var owner = CreateUser("owner@example.com", "Owner");
        var team = CreateTeam(owner.Id);
        var list = CreateGamesList(team.Id, "Backlog");
        await SeedAsync(
            owner,
            team,
            CreateAcceptedMembership(team.Id, owner.Id),
            CreateGame(1001, "Game One"),
            CreateGame(1002, "Game Two"),
            list
        );
        await SeedEntriesInOrder(
            list.Id,
            (1002, new DateTime(2026, 7, 18, 11, 0, 0, DateTimeKind.Utc)),
            (1001, new DateTime(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc))
        );

        await using var dbContext = _database.CreateDbContext();
        var context = CreateHttpContext(owner.Id);
        var endpoint = Factory.Create<GetGamesListEndpoint>(context, dbContext);
        await endpoint.HandleAsync(new GetGamesListRequest { TeamId = team.Id, ListId = list.Id }, default);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        var response = await ReadResponseAsync<GamesListDetailsResponse>(context);
        Assert.Equal(list.Id, response.Id);
        Assert.Equal("Backlog", response.Name);
        Assert.Equal(2, response.Games.Length);
        Assert.Equal(1002, response.Games[0].GameId);
        Assert.Equal("Game Two", response.Games[0].Name);
        Assert.Equal("https://images.example.com/1002-small.png", response.Games[0].CoverSmallUrl);
        Assert.Equal("https://images.example.com/1002-large.png", response.Games[0].CoverLargeUrl);
        Assert.Null(response.Games[0].GameType);
        Assert.Equal("todo", response.Games[0].State);
        Assert.Equal(1001, response.Games[1].GameId);
        Assert.Equal("todo", response.Games[1].State);
    }

    [Fact]
    public async Task GetGamesList_ReturnsNotFoundForUserWithoutAcceptedMembership()
    {
        await _database.ResetAsync();
        var owner = CreateUser("owner@example.com", "Owner");
        var outsider = CreateUser("outsider@example.com", "Outsider");
        var team = CreateTeam(owner.Id);
        var list = CreateGamesList(team.Id, "Backlog");
        await SeedAsync(owner, outsider, team, CreateAcceptedMembership(team.Id, owner.Id), list);

        await using var dbContext = _database.CreateDbContext();
        var context = CreateHttpContext(outsider.Id);
        var endpoint = Factory.Create<GetGamesListEndpoint>(context, dbContext);
        await endpoint.HandleAsync(new GetGamesListRequest { TeamId = team.Id, ListId = list.Id }, default);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task UpdateGamesList_UpdatesNameAndPicture()
    {
        await _database.ResetAsync();
        var owner = CreateUser("owner@example.com", "Owner");
        var team = CreateTeam(owner.Id);
        var list = CreateGamesList(team.Id, "Backlog", CreatePng(100, 100));
        await SeedAsync(owner, team, CreateAcceptedMembership(team.Id, owner.Id), list);
        var png = CreatePng(300, 100);
        var formFile = new FormFile(new MemoryStream(png), 0, png.Length, "Picture", "list.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png",
        };

        await using var dbContext = _database.CreateDbContext();
        var context = CreateHttpContext(owner.Id);
        var endpoint = Factory.Create<UpdateGamesListEndpoint>(
            context,
            dbContext,
            _dateTimeProvider,
            new GamesListPictureProcessor()
        );
        await endpoint.HandleAsync(
            new UpdateGamesListRequest
            {
                TeamId = team.Id,
                ListId = list.Id,
                Name = "  Finished  ",
                Picture = formFile,
            },
            default
        );

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        var response = await ReadResponseAsync<GamesListDetailsResponse>(context);
        Assert.Equal("Finished", response.Name);
        Assert.NotNull(response.Picture);

        await using var verifyContext = _database.CreateDbContext();
        var stored = await verifyContext.GamesLists.SingleAsync();
        Assert.Equal("Finished", stored.Name);
        Assert.Equal(Now, stored.UpdatedAt);
        Assert.NotNull(stored.Picture);
        using var imageStream = new MemoryStream(stored.Picture);
        using var codec = SKCodec.Create(imageStream);
        Assert.NotNull(codec);
        Assert.Equal(SKEncodedImageFormat.Webp, codec.EncodedFormat);
    }

    [Fact]
    public async Task UpdateGamesList_CanRemovePictureWithoutRenaming()
    {
        await _database.ResetAsync();
        var owner = CreateUser("owner@example.com", "Owner");
        var team = CreateTeam(owner.Id);
        var list = CreateGamesList(team.Id, "Backlog", CreatePng(100, 100));
        await SeedAsync(owner, team, CreateAcceptedMembership(team.Id, owner.Id), list);

        await using var dbContext = _database.CreateDbContext();
        var context = CreateHttpContext(owner.Id);
        var endpoint = Factory.Create<UpdateGamesListEndpoint>(
            context,
            dbContext,
            _dateTimeProvider,
            new GamesListPictureProcessor()
        );
        await endpoint.HandleAsync(
            new UpdateGamesListRequest { TeamId = team.Id, ListId = list.Id, RemovePicture = true },
            default
        );

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        var response = await ReadResponseAsync<GamesListDetailsResponse>(context);
        Assert.Equal("Backlog", response.Name);
        Assert.Null(response.Picture);

        await using var verifyContext = _database.CreateDbContext();
        var stored = await verifyContext.GamesLists.SingleAsync();
        Assert.Null(stored.Picture);
    }

    [Fact]
    public async Task UpdateGamesList_ReturnsNotFoundForUserWithoutAcceptedMembership()
    {
        await _database.ResetAsync();
        var owner = CreateUser("owner@example.com", "Owner");
        var outsider = CreateUser("outsider@example.com", "Outsider");
        var team = CreateTeam(owner.Id);
        var list = CreateGamesList(team.Id, "Backlog");
        await SeedAsync(owner, outsider, team, CreateAcceptedMembership(team.Id, owner.Id), list);

        var status = await UpdateListStatusAsync(outsider.Id, team.Id, list.Id);

        Assert.Equal(StatusCodes.Status404NotFound, status);
        await using var dbContext = _database.CreateDbContext();
        Assert.Equal("Backlog", (await dbContext.GamesLists.SingleAsync()).Name);
    }

    [Fact]
    public async Task DeleteGamesList_RemovesListAndEntries()
    {
        await _database.ResetAsync();
        var owner = CreateUser("owner@example.com", "Owner");
        var outsider = CreateUser("outsider@example.com", "Outsider");
        var team = CreateTeam(owner.Id);
        var list = CreateGamesList(team.Id, "Backlog");
        await SeedAsync(
            owner,
            outsider,
            team,
            CreateAcceptedMembership(team.Id, owner.Id),
            CreateGame(1001, "Game One"),
            CreateGame(1002, "Game Two"),
            list,
            CreateEntry(list.Id, 1001),
            CreateEntry(list.Id, 1002)
        );

        var outsiderStatus = await DeleteListStatusAsync(outsider.Id, team.Id, list.Id);
        Assert.Equal(StatusCodes.Status404NotFound, outsiderStatus);

        var status = await DeleteListStatusAsync(owner.Id, team.Id, list.Id);

        Assert.Equal(StatusCodes.Status204NoContent, status);
        await using var dbContext = _database.CreateDbContext();
        Assert.Empty(await dbContext.GamesLists.ToArrayAsync());
        Assert.Empty(await dbContext.GamesListEntries.ToArrayAsync());

        var secondStatus = await DeleteListStatusAsync(owner.Id, team.Id, list.Id);
        Assert.Equal(StatusCodes.Status404NotFound, secondStatus);
    }

    [Fact]
    public async Task AddGamesListEntry_IsIdempotentAndValidatesGame()
    {
        await _database.ResetAsync();
        var owner = CreateUser("owner@example.com", "Owner");
        var outsider = CreateUser("outsider@example.com", "Outsider");
        var team = CreateTeam(owner.Id);
        var list = CreateGamesList(team.Id, "Backlog");
        await SeedAsync(
            owner,
            outsider,
            team,
            CreateAcceptedMembership(team.Id, owner.Id),
            CreateGame(1001, "Game One"),
            list
        );

        var status = await AddEntryStatusAsync(owner.Id, team.Id, list.Id, 1001);

        Assert.Equal(StatusCodes.Status204NoContent, status);
        await using (var verifyContext = _database.CreateDbContext())
        {
            Assert.Single(await verifyContext.GamesListEntries.ToArrayAsync());
            Assert.Equal(Now, (await verifyContext.GamesLists.SingleAsync()).UpdatedAt);
        }

        var duplicateStatus = await AddEntryStatusAsync(owner.Id, team.Id, list.Id, 1001);
        Assert.Equal(StatusCodes.Status204NoContent, duplicateStatus);

        var missingGameStatus = await AddEntryStatusAsync(owner.Id, team.Id, list.Id, 9999);
        Assert.Equal(StatusCodes.Status404NotFound, missingGameStatus);

        var outsiderStatus = await AddEntryStatusAsync(outsider.Id, team.Id, list.Id, 1001);
        Assert.Equal(StatusCodes.Status404NotFound, outsiderStatus);

        await using var dbContext = _database.CreateDbContext();
        Assert.Single(await dbContext.GamesListEntries.ToArrayAsync());
    }

    [Fact]
    public async Task UpdateGamesListEntryState_ChangesStateAndTouchesList()
    {
        await _database.ResetAsync();
        var owner = CreateUser("owner@example.com", "Owner");
        var outsider = CreateUser("outsider@example.com", "Outsider");
        var team = CreateTeam(owner.Id);
        var list = CreateGamesList(team.Id, "Backlog");
        await SeedAsync(
            owner,
            outsider,
            team,
            CreateAcceptedMembership(team.Id, owner.Id),
            CreateGame(1001, "Game One"),
            list,
            CreateEntry(list.Id, 1001)
        );

        var status = await UpdateEntryStateAsync(owner.Id, team.Id, list.Id, 1001, "in-progress");

        Assert.Equal(StatusCodes.Status204NoContent, status);
        await using (var verifyContext = _database.CreateDbContext())
        {
            var entry = await verifyContext.GamesListEntries.SingleAsync();
            Assert.Equal(GamesListEntryState.InProgress, entry.State);
            Assert.Equal(Now, (await verifyContext.GamesLists.SingleAsync()).UpdatedAt);
        }

        var missingStatus = await UpdateEntryStateAsync(owner.Id, team.Id, list.Id, 9999, "completed");
        Assert.Equal(StatusCodes.Status404NotFound, missingStatus);

        var outsiderStatus = await UpdateEntryStateAsync(outsider.Id, team.Id, list.Id, 1001, "completed");
        Assert.Equal(StatusCodes.Status404NotFound, outsiderStatus);

        await using var dbContext = _database.CreateDbContext();
        Assert.Equal(GamesListEntryState.InProgress, (await dbContext.GamesListEntries.SingleAsync()).State);
    }

    [Fact]
    public async Task RemoveGamesListEntry_RemovesEntryAndReturnsNotFoundWhenMissing()
    {
        await _database.ResetAsync();
        var owner = CreateUser("owner@example.com", "Owner");
        var team = CreateTeam(owner.Id);
        var list = CreateGamesList(team.Id, "Backlog");
        await SeedAsync(
            owner,
            team,
            CreateAcceptedMembership(team.Id, owner.Id),
            CreateGame(1001, "Game One"),
            list,
            CreateEntry(list.Id, 1001)
        );

        var status = await RemoveEntryStatusAsync(owner.Id, team.Id, list.Id, 1001);

        Assert.Equal(StatusCodes.Status204NoContent, status);
        await using (var verifyContext = _database.CreateDbContext())
        {
            Assert.Empty(await verifyContext.GamesListEntries.ToArrayAsync());
            Assert.Equal(Now, (await verifyContext.GamesLists.SingleAsync()).UpdatedAt);
        }

        var missingStatus = await RemoveEntryStatusAsync(owner.Id, team.Id, list.Id, 1001);
        Assert.Equal(StatusCodes.Status404NotFound, missingStatus);
    }

    private async Task<int> CreateListStatusAsync(Guid userId, Guid teamId)
    {
        await using var dbContext = _database.CreateDbContext();
        var context = CreateHttpContext(userId);
        var endpoint = Factory.Create<CreateGamesListEndpoint>(
            context,
            dbContext,
            _dateTimeProvider,
            new GamesListPictureProcessor()
        );
        await endpoint.HandleAsync(new CreateGamesListRequest { TeamId = teamId, Name = "Backlog" }, default);
        return context.Response.StatusCode;
    }

    private async Task<GamesListSummaryResponse[]> ListGamesListsAsync(Guid userId, Guid teamId, long? gameId)
    {
        await using var dbContext = _database.CreateDbContext();
        var context = CreateHttpContext(userId);
        var endpoint = Factory.Create<ListGamesListsEndpoint>(context, dbContext);
        await endpoint.HandleAsync(new ListGamesListsRequest { TeamId = teamId, GameId = gameId }, default);
        return await ReadResponseAsync<GamesListSummaryResponse[]>(context);
    }

    private async Task<int> UpdateListStatusAsync(Guid userId, Guid teamId, Guid listId)
    {
        await using var dbContext = _database.CreateDbContext();
        var context = CreateHttpContext(userId);
        var endpoint = Factory.Create<UpdateGamesListEndpoint>(
            context,
            dbContext,
            _dateTimeProvider,
            new GamesListPictureProcessor()
        );
        await endpoint.HandleAsync(
            new UpdateGamesListRequest { TeamId = teamId, ListId = listId, Name = "Renamed" },
            default
        );
        return context.Response.StatusCode;
    }

    private async Task<int> DeleteListStatusAsync(Guid userId, Guid teamId, Guid listId)
    {
        await using var dbContext = _database.CreateDbContext();
        var context = CreateHttpContext(userId);
        var endpoint = Factory.Create<DeleteGamesListEndpoint>(context, dbContext);
        await endpoint.HandleAsync(new DeleteGamesListRequest { TeamId = teamId, ListId = listId }, default);
        return context.Response.StatusCode;
    }

    private async Task<int> AddEntryStatusAsync(Guid userId, Guid teamId, Guid listId, long gameId)
    {
        await using var dbContext = _database.CreateDbContext();
        var context = CreateHttpContext(userId);
        var endpoint = Factory.Create<AddGamesListEntryEndpoint>(context, dbContext, _dateTimeProvider);
        await endpoint.HandleAsync(
            new AddGamesListEntryRequest { TeamId = teamId, ListId = listId, GameId = gameId },
            default
        );
        return context.Response.StatusCode;
    }

    private async Task<int> UpdateEntryStateAsync(Guid userId, Guid teamId, Guid listId, long gameId, string state)
    {
        await using var dbContext = _database.CreateDbContext();
        var context = CreateHttpContext(userId);
        var endpoint = Factory.Create<UpdateGamesListEntryStateEndpoint>(context, dbContext, _dateTimeProvider);
        await endpoint.HandleAsync(
            new UpdateGamesListEntryStateRequest
            {
                TeamId = teamId,
                ListId = listId,
                GameId = gameId,
                State = state,
            },
            default
        );
        return context.Response.StatusCode;
    }

    private async Task<int> RemoveEntryStatusAsync(Guid userId, Guid teamId, Guid listId, long gameId)
    {
        await using var dbContext = _database.CreateDbContext();
        var context = CreateHttpContext(userId);
        var endpoint = Factory.Create<RemoveGamesListEntryEndpoint>(context, dbContext, _dateTimeProvider);
        await endpoint.HandleAsync(
            new RemoveGamesListEntryRequest { TeamId = teamId, ListId = listId, GameId = gameId },
            default
        );
        return context.Response.StatusCode;
    }

    private async Task SeedAsync(params object[] entities)
    {
        await using var dbContext = _database.CreateDbContext();
        dbContext.AddRange(entities);
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedEntriesInOrder(Guid listId, params (long GameId, DateTime AddedAt)[] entries)
    {
        await using var dbContext = _database.CreateDbContext();
        foreach (var (gameId, addedAt) in entries)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "public"."GamesListEntries" ("GamesListId", "GameId", "AddedAt")
                VALUES ({listId}, {gameId}, {addedAt})
                """
            );
        }
    }

    private static DefaultHttpContext CreateHttpContext(Guid userId)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("ApesDbUserId", userId.ToString())], "Test"));
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<T> ReadResponseAsync<T>(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        var response = await JsonSerializer.DeserializeAsync<T>(
            context.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
        );
        return Assert.IsType<T>(response);
    }

    private static User CreateUser(string email, string name)
    {
        return new User
        {
            Id = Guid.CreateVersion7(),
            Auth0Subject = $"auth0|{Guid.NewGuid():N}",
            Email = email,
            Name = name,
            PictureUrl = $"https://images.example.com/{email}.png",
            CreatedAt = Now,
            UpdatedAt = Now,
        };
    }

    private static Team CreateTeam(Guid ownerId, TeamKind kind = TeamKind.Group)
    {
        return new Team
        {
            Id = Guid.CreateVersion7(),
            OwnerUserId = ownerId,
            Name = "Apes",
            Kind = kind,
            CreatedAt = Now,
            UpdatedAt = Now,
        };
    }

    private static TeamMembership CreateAcceptedMembership(Guid teamId, Guid userId)
    {
        return new TeamMembership
        {
            Id = Guid.CreateVersion7(),
            TeamId = teamId,
            UserId = userId,
            Status = TeamMembershipStatus.Accepted,
            InvitedAt = Now,
            AcceptedAt = Now,
        };
    }

    private static GamesList CreateGamesList(Guid teamId, string name, byte[]? picture = null)
    {
        return new GamesList
        {
            Id = Guid.CreateVersion7(),
            TeamId = teamId,
            Name = name,
            Picture = picture,
            CreatedAt = Now,
            UpdatedAt = Now,
        };
    }

    private static Game CreateGame(long id, string name)
    {
        return new Game
        {
            Id = id,
            Name = name,
            CoverSmallUrl = $"https://images.example.com/{id}-small.png",
            CoverLargeUrl = $"https://images.example.com/{id}-large.png",
        };
    }

    private static GamesListEntry CreateEntry(Guid listId, long gameId)
    {
        return new GamesListEntry { GamesListId = listId, GameId = gameId };
    }

    private static byte[] CreatePng(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
