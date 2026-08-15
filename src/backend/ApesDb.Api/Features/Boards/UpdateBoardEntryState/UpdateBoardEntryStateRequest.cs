using System.Text.Json.Serialization;

namespace ApesDb.Api.Features.Boards.UpdateBoardEntryState;

public sealed class UpdateBoardEntryStateRequest
{
    public Guid BoardId { get; init; }

    public long GameId { get; init; }

    public string State { get; init; } = string.Empty;

    [JsonRequired]
    public int Position { get; init; }
}
