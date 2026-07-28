using ApesDb.Api.Features.Boards.UpdateBoardEntryState;

namespace ApesDb.Api.UnitTests.Features.Boards.UpdateBoardEntryState;

public sealed class UpdateBoardEntryStateValidatorTests
{
    [Fact]
    public void ValidateAcceptsStateWithinStorageLimit()
    {
        var validator = new UpdateBoardEntryStateValidator();
        var result = validator.Validate(new UpdateBoardEntryStateRequest { State = "database-state", Position = 0 });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("state-name-longer-than-sixteen-characters")]
    public void ValidateRejectsInvalidStateName(string state)
    {
        var validator = new UpdateBoardEntryStateValidator();
        var result = validator.Validate(new UpdateBoardEntryStateRequest { State = state, Position = 0 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateBoardEntryStateRequest.State));
    }

    [Fact]
    public void ValidateRejectsNegativePosition()
    {
        var validator = new UpdateBoardEntryStateValidator();
        var result = validator.Validate(new UpdateBoardEntryStateRequest { State = "todo", Position = -1 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateBoardEntryStateRequest.Position));
    }
}
