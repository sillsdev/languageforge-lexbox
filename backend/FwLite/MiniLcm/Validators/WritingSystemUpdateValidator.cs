using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

public class WritingSystemUpdateValidator : AbstractValidator<UpdateObjectInput<WritingSystem>>
{
    public WritingSystemUpdateValidator()
    {
        RuleFor(u => u.Patch).DoesNotChangeProperty(nameof(WritingSystem.WsId));
        RuleFor(u => u.Patch).DoesNotChangeProperty(nameof(WritingSystem.Type));
        RuleFor(u => u.Patch).DoesNotChangeProperty(nameof(WritingSystem.DeletedAt));
        RuleFor(u => u.Patch.Operations).NotEmpty();
    }
}
