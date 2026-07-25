using FastEndpoints;
using FluentValidation;

namespace ApesDb.Api.Features.Boards.ListBoards;

public sealed class ListBoardsValidator : Validator<ListBoardsRequest>
{
    public ListBoardsValidator()
    {
        RuleFor(request => request.Page).GreaterThanOrEqualTo(1);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
    }
}
