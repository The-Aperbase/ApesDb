using System.Text.Json;
using ApesDb.Api.Tests.Infrastructure.Http;

namespace ApesDb.Api.Tests.Features.Boards;

internal static class BoardTestSupport
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static readonly byte[] ValidPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="
    );

    public static string BoardUrl(Guid boardId)
    {
        return $"/api/boards/{boardId}";
    }

    public static string EntriesUrl(Guid boardId)
    {
        return $"{BoardUrl(boardId)}/entries";
    }

    public static string EntryUrl(Guid boardId, long gameId)
    {
        return $"{EntriesUrl(boardId)}/{gameId}";
    }

    public static MultipartFormDataContent CreateForm(
        string? name = null,
        byte[]? picture = null,
        string pictureContentType = "image/png",
        bool removePicture = false
    )
    {
        var form = new MultipartFormDataContent();
        if (name is not null)
        {
            form.Add(new StringContent(name), "Name");
        }

        if (picture is not null)
        {
            var file = new ByteArrayContent(picture);
            file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(pictureContentType);
            form.Add(file, "Picture", "board.png");
        }

        if (removePicture)
        {
            form.Add(new StringContent("true"), "RemovePicture");
        }

        return form;
    }

    public static async Task<BoardSummaryContract> ReadSummaryAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<BoardSummaryContract>(content, SerializerOptions)
            ?? throw new InvalidOperationException("The board summary response was empty.");
    }

    public static async Task<HttpResponseSnapshot> SummarySnapshotAsync(
        HttpResponseMessage response,
        Guid? createdBoardId = null
    )
    {
        var raw = await HttpResponseSnapshot.CreateAsync<BoardSummaryContract>(response);
        var content = (BoardSummaryContract?)raw.Content;
        return new HttpResponseSnapshot(raw.Response, content?.ToSnapshot(createdBoardId));
    }

    public static async Task<HttpResponseSnapshot> ListSnapshotAsync(
        HttpResponseMessage response,
        Guid? createdBoardId = null
    )
    {
        var raw = await HttpResponseSnapshot.CreateAsync<PagableContract<BoardSummaryContract>>(response);
        var content = (PagableContract<BoardSummaryContract>?)raw.Content;
        return new HttpResponseSnapshot(raw.Response, content?.ToSnapshot(board => board.ToSnapshot(createdBoardId)));
    }

    public static async Task<HttpResponseSnapshot> DetailsSnapshotAsync(
        HttpResponseMessage response,
        Guid? createdBoardId = null
    )
    {
        var raw = await HttpResponseSnapshot.CreateAsync<BoardDetailsContract>(response);
        var content = (BoardDetailsContract?)raw.Content;
        return new HttpResponseSnapshot(raw.Response, content?.ToSnapshot(createdBoardId));
    }
}

internal sealed record BoardPictureContract(string ContentType, byte[] Data)
{
    public BoardPictureSnapshot ToSnapshot()
    {
        return new BoardPictureSnapshot(ContentType, Data.Length);
    }
}

internal sealed record PagableContract<T>(T[] Items, int Total, int FilteredTotal, int Page, int PageSize)
{
    public PagableContract<TSnapshot> ToSnapshot<TSnapshot>(Func<T, TSnapshot> createSnapshot)
    {
        return new PagableContract<TSnapshot>(
            Items.Select(createSnapshot).ToArray(),
            Total,
            FilteredTotal,
            Page,
            PageSize
        );
    }
}

internal sealed record BoardSummaryContract(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    BoardPictureContract? Picture,
    int GameCount,
    bool ContainsGame
)
{
    public BoardSummarySnapshot ToSnapshot(Guid? createdBoardId = null)
    {
        object id = Id;
        if (createdBoardId == Id)
        {
            id = "{created-board-id}";
        }

        return new BoardSummarySnapshot(id, Name, CreatedAt, UpdatedAt, Picture?.ToSnapshot(), GameCount, ContainsGame);
    }
}

internal sealed record BoardGameContract(
    long GameId,
    string Name,
    string? CoverSmallUrl,
    string? CoverLargeUrl,
    string? GameType,
    DateTime AddedAt
);

internal sealed record BoardDetailsContract(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    BoardPictureContract? Picture,
    Dictionary<string, Dictionary<int, BoardGameContract>> Games
)
{
    public BoardDetailsSnapshot ToSnapshot(Guid? createdBoardId = null)
    {
        object id = Id;
        if (createdBoardId == Id)
        {
            id = "{created-board-id}";
        }

        return new BoardDetailsSnapshot(id, Name, CreatedAt, UpdatedAt, Picture?.ToSnapshot(), Games);
    }
}

internal sealed record BoardPictureSnapshot(string ContentType, int ByteLength);

internal sealed record BoardSummarySnapshot(
    object Id,
    string Name,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    BoardPictureSnapshot? Picture,
    int GameCount,
    bool ContainsGame
);

internal sealed record BoardDetailsSnapshot(
    object Id,
    string Name,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    BoardPictureSnapshot? Picture,
    Dictionary<string, Dictionary<int, BoardGameContract>> Games
);
