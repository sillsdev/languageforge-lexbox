using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

public class SemanticDomainValidator : AbstractValidator<SemanticDomain>
{
    public SemanticDomainValidator()
    {
        RuleFor(s => s.Code).NotNull().NotEmpty();
        RuleFor(s => s.DeletedAt).Null();
        RuleFor(s => s.Id).Must(BeCanonicalGuid).When(s => s.Predefined);
        RuleFor(s => s.Name).Required();
    }

    private bool BeCanonicalGuid(Guid id)
    {
        // TODO: Load SemDom.xml into app as a resource and add singleton providing access to it, then look up GUIDs there
        return true;
    }
}
