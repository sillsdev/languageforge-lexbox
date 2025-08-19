using FluentValidation;
using MiniLcm.Models;
using SIL.WritingSystems;

namespace MiniLcm.Validators;

public class WritingSystemValidator : AbstractValidator<WritingSystem>
{
    public WritingSystemValidator()
    {
        RuleFor(ws => ws.Abbreviation).NotNull().NotEmpty().WithMessage((s) => $"Abbreviation is required ({s.WsId} - {s.Name} - {s.Id})");
        RuleFor(ws => ws.DeletedAt).Null();
        RuleFor(ws => ws.Name).NotNull().NotEmpty().WithMessage((s) => $"Name is required ({s.WsId} - {s.Id})");
        RuleFor(ws => ws.WsId).Must(BeValidWsId).WithMessage(ws => $"Invalid writing system id: {ws.WsId}");
    }

    private bool BeValidWsId(WritingSystemId wsId)
    {
        return wsId.Code == "default" || wsId.Code == "__key" || IetfLanguageTag.IsValid(wsId.Code);
    }
}
