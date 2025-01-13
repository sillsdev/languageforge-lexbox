using FluentValidation;
using MiniLcm.Models;
using SIL.WritingSystems;

namespace MiniLcm.Validators;

public class WritingSystemValidator : AbstractValidator<WritingSystem>
{
    public WritingSystemValidator()
    {
        RuleFor(ws => ws.Abbreviation).NotNull().NotEmpty();
        RuleFor(ws => ws.DeletedAt).Null();
        RuleFor(ws => ws.Name).NotNull().NotEmpty();
        RuleFor(ws => ws.WsId).Must(BeValidWsId);
    }

    private bool BeValidWsId(WritingSystemId wsId)
    {
        return wsId.Code == "default" || wsId.Code == "__key" || IetfLanguageTag.IsValid(wsId.Code);
    }
}
