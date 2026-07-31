using ApesDb.Domain.Entities.Boards;
using FastEndpoints;
using FluentValidation;

namespace ApesDb.Api.Features.Boards.Sharing.CreateBoardInvitation;

public sealed class CreateBoardInvitationValidator : Validator<CreateBoardInvitationRequest>
{
    public CreateBoardInvitationValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .MaximumLength(BoardInvitation.MaximumEmailLength)
            .EmailAddress();
    }
}
