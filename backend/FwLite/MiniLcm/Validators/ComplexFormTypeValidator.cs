using FluentValidation;
using FluentValidation.Validators;
using MiniLcm.Models;

namespace MiniLcm.Validators;

internal class ComplexFormTypeValidator : AbstractValidator<ComplexFormType>
{
    public ComplexFormTypeValidator()
    {
        RuleFor(c => c.DeletedAt).Null();
        RuleFor(c => c.Name).Required();
    }
}
