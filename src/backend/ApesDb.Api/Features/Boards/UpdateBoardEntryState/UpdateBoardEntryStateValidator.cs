using FastEndpoints;
using FluentValidation;

namespace ApesDb.Api.Features.Boards.UpdateBoardEntryState;

public sealed class UpdateBoardEntryStateValidator : Validator<UpdateBoardEntryStateRequest>
{
    public UpdateBoardEntryStateValidator()
    {
        RuleFor(request => request.State)
            .Must(state => state == "todo" || state == "in-progress" || state == "completed" || state == "dnf")
            .WithMessage("State must be one of: todo, in-progress, completed, dnf.");
    }
}
