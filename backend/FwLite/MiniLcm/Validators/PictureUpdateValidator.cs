using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

public class PictureUpdateValidator : AbstractValidator<UpdateObjectInput<Picture>>
{
    public PictureUpdateValidator()
    {
        RuleFor(u => u.Patch).DoesNotChangeProperty(nameof(Picture.Order));
    }
}
