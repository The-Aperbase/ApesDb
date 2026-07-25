namespace ApesDb.Api.Features.Boards.CreateBoard;

public sealed class CreateBoardValidator : BoardValidator<CreateBoardRequest>
{
    public CreateBoardValidator()
        : base(nameIsRequired: true)
    {
    }
}
