using ApesDb.Api.Features.Boards.CreateBoard;
using Microsoft.AspNetCore.Http;

namespace ApesDb.Api.UnitTests.Features.Boards.CreateBoard;

public sealed class CreateBoardValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateRejectsEmptyOrWhitespaceName(string name)
    {
        var validator = new CreateBoardValidator();
        var result = validator.Validate(new CreateBoardRequest { Name = name });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateBoardRequest.Name));
    }

    [Fact]
    public void ValidateAcceptsPictureAtMaximumLength()
    {
        var validator = new CreateBoardValidator();
        var picture = CreateFormFile(CreateBoardValidator.MaximumPictureLength);

        var result = validator.Validate(new CreateBoardRequest { Name = "Backlog", Picture = picture });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateRejectsPictureOverMaximumLength()
    {
        var validator = new CreateBoardValidator();
        var picture = CreateFormFile(CreateBoardValidator.MaximumPictureLength + 1);

        var result = validator.Validate(new CreateBoardRequest { Name = "Backlog", Picture = picture });

        var error = Assert.Single(result.Errors, error => error.PropertyName == nameof(CreateBoardRequest.Picture));
        Assert.Equal("Picture must not exceed 5 MB.", error.ErrorMessage);
    }

    private static IFormFile CreateFormFile(long length)
    {
        return new FormFile(Stream.Null, 0, length, "Picture", "board.png");
    }
}
