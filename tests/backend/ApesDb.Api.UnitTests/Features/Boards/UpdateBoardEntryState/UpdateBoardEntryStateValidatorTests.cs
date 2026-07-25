using ApesDb.Api.Features.Boards.UpdateBoardEntryState;

namespace ApesDb.Api.UnitTests.Features.Boards.UpdateBoardEntryState;

public sealed class UpdateBoardEntryStateValidatorTests
{
    [Fact]
    public void ValidateAcceptsStateWithinStorageLimit()
    {
        var validator = new UpdateBoardEntryStateValidator();
        var result = validator.Validate(new UpdateBoardEntryStateRequest { State = "database-state" });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("state-name-longer-than-sixteen-characters")]
    public void ValidateRejectsInvalidStateName(string state)
    {
        var validator = new UpdateBoardEntryStateValidator();
        var result = validator.Validate(new UpdateBoardEntryStateRequest { State = state });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateBoardEntryStateRequest.State));
    }
}
