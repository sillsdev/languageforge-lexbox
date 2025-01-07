using FluentValidation;

namespace MiniLcm.Validators;

public class PartOfSpeechIdValidator : AbstractValidator<Guid?>
{
    public PartOfSpeechIdValidator()
    {
        RuleFor(id => id).Must(BeCanonicalGuid);
    }

    private bool BeCanonicalGuid(Guid? id)
    {
        return id == null || CanonicalGuidsPartOfSpeech.CanonicalPosGuids.Contains(id.Value);
    }
}
