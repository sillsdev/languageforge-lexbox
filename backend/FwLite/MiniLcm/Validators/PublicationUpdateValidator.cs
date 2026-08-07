using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

public class PublicationUpdateValidator : AbstractValidator<UpdateObjectInput<Publication>>
{
    public PublicationUpdateValidator()
    {
        RuleFor(u => u.Patch).DoesNotChangeProperty(nameof(Publication.Id));
        RuleFor(u => u.Patch).DoesNotChangeProperty(nameof(Publication.DeletedAt));
        RuleFor(u => u.Patch).DoesNotChangePropertyTo(
            nameof(Publication.IsMain),
            false,
            "Cannot turn off the IsMain flag on a publication; the main publication is fixed.");
        RuleFor(u => u.Patch.Operations).NotEmpty();
    }
}
