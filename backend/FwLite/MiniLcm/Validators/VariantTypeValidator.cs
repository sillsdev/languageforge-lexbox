using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

internal class VariantTypeValidator : AbstractValidator<VariantType>
{
    public VariantTypeValidator()
    {
        RuleFor(c => c.DeletedAt).Null();
        RuleFor(c => c.Name).Required(c => c.Id.ToString("D"));
    }
}
