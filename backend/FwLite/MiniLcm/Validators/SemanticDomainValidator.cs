using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

public class SemanticDomainValidator : AbstractValidator<SemanticDomain>
{
    public SemanticDomainValidator()
    {
        RuleFor(s => s)
            .Must(s => !string.IsNullOrEmpty(SemanticDomain.ResolveCode(s.Abbreviation, s.Code)))
            .WithMessage(s => $"Code or Abbreviation is required ({s.Name} - {s.Id})");
        RuleFor(s => s.DeletedAt).Null();
        RuleFor(s => s.Id).Must(BeCanonicalGuid).When(s => s.Predefined);
        RuleFor(s => s.Name).Required(s => s.Id.ToString("D"));
    }

    private bool BeCanonicalGuid(Guid id)
    {
        return CanonicalGuidsSemanticDomain.CanonicalSemDomGuids.Contains(id);
    }
}
