using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

internal class ComplexFormTypeValidator : AbstractValidator<ComplexFormType>
{
    public ComplexFormTypeValidator()
    {
        RuleFor(c => c.DeletedAt).Null();
        RuleFor(c => c.Name).Required(c => c.Id.ToString("D"));
    }
}
