using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

public class MorphTypeUpdateValidator : AbstractValidator<UpdateObjectInput<MorphType>>
{
    public MorphTypeUpdateValidator()
    {
        RuleFor(u => u.Patch).DoesNotChangeProperty(nameof(MorphType.Kind));
        RuleFor(u => u.Patch).DoesNotChangeProperty(nameof(MorphType.DeletedAt));
        RuleFor(u => u.Patch.Operations).NotEmpty();
    }
}
