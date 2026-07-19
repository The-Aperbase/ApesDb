using ApesDb.Api.Features.GamesLists.CreateGamesList;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace ApesDb.GamesLists.IntegrationTests;

public sealed class CreateGamesListValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_RejectsEmptyOrWhitespaceName(string name)
    {
        var validator = new CreateGamesListValidator();
        var result = validator.Validate(new CreateGamesListRequest { Name = name });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateGamesListRequest.Name));
    }

    [Fact]
    public void Validate_AcceptsPictureAtMaximumLength()
    {
        var validator = new CreateGamesListValidator();
        var picture = CreateFormFile(CreateGamesListValidator.MaximumPictureLength);

        var result = validator.Validate(new CreateGamesListRequest { Name = "Backlog", Picture = picture });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsPictureOverMaximumLength()
    {
        var validator = new CreateGamesListValidator();
        var picture = CreateFormFile(CreateGamesListValidator.MaximumPictureLength + 1);

        var result = validator.Validate(new CreateGamesListRequest { Name = "Backlog", Picture = picture });

        var error = Assert.Single(
            result.Errors,
            error => error.PropertyName == nameof(CreateGamesListRequest.Picture)
        );
        Assert.Equal("Picture must not exceed 5 MB.", error.ErrorMessage);
    }

    private static IFormFile CreateFormFile(long length)
    {
        return new FormFile(Stream.Null, 0, length, "Picture", "list.png");
    }
}
