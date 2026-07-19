using FastEndpoints;
using FluentValidation;

namespace ApesDb.Api.Features.GamesLists.CreateGamesList;

public sealed class CreateGamesListValidator : Validator<CreateGamesListRequest>
{
    public const long MaximumPictureLength = 5 * 1024 * 1024;

    public CreateGamesListValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(128);
        RuleFor(request => request.Picture)
            .Must(picture => picture is null || picture.Length <= MaximumPictureLength)
            .WithMessage("Picture must not exceed 5 MB.");
    }
}
