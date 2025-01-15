using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

public class OrderableValidator : AbstractValidator<IOrderable>
{
    public OrderableValidator()
    {
        RuleFor(o => o.Order).Equal(default(double)).WithMessage("Order must not be set explicitly, it is managed internally.");
    }
}
