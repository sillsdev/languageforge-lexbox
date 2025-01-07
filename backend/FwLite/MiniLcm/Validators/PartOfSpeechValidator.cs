using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

public class PartOfSpeechValidator : AbstractValidator<PartOfSpeech>
{
    public PartOfSpeechValidator()
    {
        RuleFor(pos => pos.Id).Must(BeCanonicalGuid).When(pos => pos.Predefined);
        RuleFor(pos => pos.DeletedAt).Null();
        RuleFor(pos => pos.Name).Required();
    }

    private bool BeCanonicalGuid(Guid id)
    {
        return CanonicalGuidsPartOfSpeech.CanonicalPosGuids.Contains(id);
    }
}
