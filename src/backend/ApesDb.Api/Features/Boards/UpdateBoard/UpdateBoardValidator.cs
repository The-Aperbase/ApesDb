namespace ApesDb.Api.Features.Boards.UpdateBoard;

public sealed class UpdateBoardValidator : BoardValidator<UpdateBoardRequest>
{
    public UpdateBoardValidator()
        : base(nameIsRequired: false) { }
}
