using FastEndpoints;
using FluentValidation;

namespace ApesDb.Api.Features.GamesLists.UpdateGamesListEntryState;

public sealed class UpdateGamesListEntryStateValidator : Validator<UpdateGamesListEntryStateRequest>
{
    public UpdateGamesListEntryStateValidator()
    {
        RuleFor(request => request.State)
            .Must(state =>
                state == "todo" || state == "in-progress" || state == "completed" || state == "dnf"
            )
            .WithMessage("State must be one of: todo, in-progress, completed, dnf.");
    }
}
