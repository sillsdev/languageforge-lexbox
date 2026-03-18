using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

public class MorphTypeValidator : AbstractValidator<MorphType>
{
    public MorphTypeValidator()
    {
        RuleFor(m => m.DeletedAt).Null();
        RuleFor(m => m.Name).NotNull().NotEmpty().WithMessage((m) => $"Name is required ({m.Kind} - {m.Id})");
        RuleFor(m => m.Abbreviation).NotNull().NotEmpty().WithMessage((m) => $"Abbreviation is required ({m.Kind} - {m.Name} - {m.Id})");
    }
}
