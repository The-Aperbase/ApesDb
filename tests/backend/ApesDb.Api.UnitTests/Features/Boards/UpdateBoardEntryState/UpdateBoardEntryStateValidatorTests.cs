using ApesDb.Api.Features.Boards.UpdateBoardEntryState;

namespace ApesDb.Api.UnitTests.Features.Boards.UpdateBoardEntryState;

public sealed class UpdateBoardEntryStateValidatorTests
{
    [Theory]
    [InlineData("todo")]
    [InlineData("in-progress")]
    [InlineData("completed")]
    [InlineData("dnf")]
    public void ValidateAcceptsKnownStates(string state)
    {
        var validator = new UpdateBoardEntryStateValidator();
        var result = validator.Validate(new UpdateBoardEntryStateRequest { State = state });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("done")]
    [InlineData("Todo")]
    public void ValidateRejectsUnknownStates(string state)
    {
        var validator = new UpdateBoardEntryStateValidator();
        var result = validator.Validate(new UpdateBoardEntryStateRequest { State = state });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateBoardEntryStateRequest.State));
    }
}
