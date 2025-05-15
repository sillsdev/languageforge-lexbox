using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

public class WritingSystemUpdateValidator : AbstractValidator<UpdateObjectInput<WritingSystem>>
{
    public WritingSystemUpdateValidator()
    {
        RuleFor(u => u.Patch).NoOperation(nameof(WritingSystem.WsId));
        RuleFor(u => u.Patch).NoOperation(nameof(WritingSystem.Type));
        RuleFor(u => u.Patch).NoOperation(nameof(WritingSystem.DeletedAt));
        RuleFor(u => u.Patch.Operations).NotEmpty();
    }
}
