using FastEndpoints;
using FluentValidation;

namespace ApesDb.Api.Features.Boards.CreateBoard;

public sealed class CreateBoardValidator : Validator<CreateBoardRequest>
{
    public const long MaximumPictureLength = 5 * 1024 * 1024;

    public CreateBoardValidator()
    {
        RuleFor(request => request.Name)
            .Must(name => name.Trim().Length > 0)
            .WithMessage("Name must not be empty.")
            .MaximumLength(128);
        RuleFor(request => request.Picture)
            .Must(picture => picture is null || picture.Length <= MaximumPictureLength)
            .WithMessage("Picture must not exceed 5 MB.");
    }
}
