using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace ApesDb.Api.Features.Boards;

public interface IBoardMutationRequest
{
    string? Name { get; }

    IFormFile? Picture { get; }
}

public abstract class BoardValidator<TRequest> : Validator<TRequest>
    where TRequest : IBoardMutationRequest
{
    private const long MaximumPictureLength = 5 * 1024 * 1024;

    protected BoardValidator(bool nameIsRequired)
    {
        var nameRule = RuleFor(request => request.Name);
        if (nameIsRequired)
        {
            nameRule.Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Name must not be empty.");
        }
        else
        {
            nameRule.Must(name => name is null || name.Trim().Length > 0).WithMessage("Name must not be empty.");
        }

        nameRule.MaximumLength(128);
        RuleFor(request => request.Picture)
            .Must(picture => picture is null || picture.Length <= MaximumPictureLength)
            .WithMessage("Picture must not exceed 5 MB.");
    }
}
