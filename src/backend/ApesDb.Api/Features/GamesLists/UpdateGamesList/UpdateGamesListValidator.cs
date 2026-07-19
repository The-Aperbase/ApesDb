using ApesDb.Api.Features.GamesLists.CreateGamesList;
using FastEndpoints;
using FluentValidation;

namespace ApesDb.Api.Features.GamesLists.UpdateGamesList;

public sealed class UpdateGamesListValidator : Validator<UpdateGamesListRequest>
{
    public UpdateGamesListValidator()
    {
        RuleFor(request => request.Name)
            .Must(name => name is null || name.Trim().Length > 0)
            .WithMessage("Name must not be empty.")
            .MaximumLength(128);
        RuleFor(request => request.Picture)
            .Must(picture => picture is null || picture.Length <= CreateGamesListValidator.MaximumPictureLength)
            .WithMessage("Picture must not exceed 5 MB.");
    }
}
