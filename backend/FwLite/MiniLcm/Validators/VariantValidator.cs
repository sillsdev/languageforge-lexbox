using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

internal class VariantValidator : AbstractValidator<Variant>
{
    public VariantValidator()
    {
        RuleFor(v => v.DeletedAt).Null();
        RuleFor(v => v.Comment).NoEmptyValues(GetIdentifier).NoDefaultWritingSystems(GetIdentifier);
        RuleForEach(v => v.Types).SetValidator(new VariantTypeValidator());
    }

    private string GetIdentifier(Variant variant)
    {
        return $"{variant.VariantHeadword} -> {variant.MainHeadword} ({variant.VariantEntryId} -> {variant.MainEntryId})";
    }
}
