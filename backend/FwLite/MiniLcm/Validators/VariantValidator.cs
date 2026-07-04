using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

internal class VariantValidator : AbstractValidator<Variant>
{
    public VariantValidator()
    {
        RuleFor(v => v.DeletedAt).Null();
        //one endpoint may be an empty guid when nested in an entry (inferred from the parent),
        //so only reject self-reference when both are set
        RuleFor(v => v)
            .Must(v => v.VariantEntryId == Guid.Empty || v.VariantEntryId != v.MainEntryId)
            .WithMessage(v => $"Variant {GetIdentifier(v)} must not be a variant of itself.");
        RuleFor(v => v.Comment).NoEmptyValues(GetIdentifier).NoDefaultWritingSystems(GetIdentifier);
        RuleForEach(v => v.Types).SetValidator(new VariantTypeValidator());
    }

    private string GetIdentifier(Variant variant)
    {
        return $"{variant.VariantHeadword} -> {variant.MainHeadword} ({variant.VariantEntryId} -> {variant.MainEntryId})";
    }
}
