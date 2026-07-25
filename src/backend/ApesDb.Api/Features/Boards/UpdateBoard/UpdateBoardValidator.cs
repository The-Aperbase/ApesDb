using ApesDb.Api.Features.Boards.CreateBoard;
using FastEndpoints;
using FluentValidation;

namespace ApesDb.Api.Features.Boards.UpdateBoard;

public sealed class UpdateBoardValidator : Validator<UpdateBoardRequest>
{
    public UpdateBoardValidator()
    {
        RuleFor(request => request.Name)
            .Must(name => name is null || name.Trim().Length > 0)
            .WithMessage("Name must not be empty.")
            .MaximumLength(128);
        RuleFor(request => request.Picture)
            .Must(picture => picture is null || picture.Length <= CreateBoardValidator.MaximumPictureLength)
            .WithMessage("Picture must not exceed 5 MB.");
    }
}
