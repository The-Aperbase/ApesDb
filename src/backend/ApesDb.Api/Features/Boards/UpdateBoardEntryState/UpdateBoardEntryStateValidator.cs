using FastEndpoints;
using FluentValidation;

namespace ApesDb.Api.Features.Boards.UpdateBoardEntryState;

public sealed class UpdateBoardEntryStateValidator : Validator<UpdateBoardEntryStateRequest>
{
    public UpdateBoardEntryStateValidator()
    {
        RuleFor(request => request.State).NotEmpty().MaximumLength(16);
        RuleFor(request => request.Position).GreaterThanOrEqualTo(0);
    }
}
