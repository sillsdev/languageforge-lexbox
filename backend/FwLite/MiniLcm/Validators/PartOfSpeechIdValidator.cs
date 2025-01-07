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
        // TODO: Load GOLDEtic.xml into app as a resource and add singleton providing access to it, then look up GUIDs there
        return true;
    }
}
