using FluentValidation;

namespace MiniLcm.Validators;

public class IsCanonicalPartOfSpeechGuidValidator : AbstractValidator<Guid?>
{
    public IsCanonicalPartOfSpeechGuidValidator()
    {
        RuleFor(id => id).Must(BeCanonicalGuid);
    }

    private bool BeCanonicalGuid(Guid? id)
    {
        return id == null || CanonicalGuidsPartOfSpeech.CanonicalPosGuids.Contains(id.Value);
    }
}
