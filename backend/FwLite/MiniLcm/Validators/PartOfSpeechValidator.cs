using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

public class PartOfSpeechValidator : AbstractValidator<PartOfSpeech>
{
    public PartOfSpeechValidator()
    {
        RuleFor(pos => pos.Id).Must(BeCanonicalGuid).When(pos => pos.Predefined);
        RuleFor(pos => pos.DeletedAt).Null();
        // This seems to block quite a few real projects
        // RuleFor(pos => pos.Name).Required(pos => pos.Id.ToString("D"));
    }

    private bool BeCanonicalGuid(Guid id)
    {
        return CanonicalGuidsPartOfSpeech.CanonicalPosGuids.Contains(id);
    }
}
