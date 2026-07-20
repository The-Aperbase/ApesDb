using ApesDb.Api.Features.GamesLists.UpdateGamesListEntryState;
using Xunit;

namespace ApesDb.GamesLists.IntegrationTests;

public sealed class UpdateGamesListEntryStateValidatorTests
{
    [Theory]
    [InlineData("todo")]
    [InlineData("in-progress")]
    [InlineData("completed")]
    [InlineData("dnf")]
    public void Validate_AcceptsKnownStates(string state)
    {
        var validator = new UpdateGamesListEntryStateValidator();
        var result = validator.Validate(new UpdateGamesListEntryStateRequest { State = state });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("done")]
    [InlineData("Todo")]
    public void Validate_RejectsUnknownStates(string state)
    {
        var validator = new UpdateGamesListEntryStateValidator();
        var result = validator.Validate(new UpdateGamesListEntryStateRequest { State = state });

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(UpdateGamesListEntryStateRequest.State)
        );
    }
}
