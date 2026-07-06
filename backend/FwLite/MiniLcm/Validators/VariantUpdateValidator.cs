using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

/// <summary>
/// Patches may only touch a link's own scalar data (HideMinorEntry, Comment). Types has
/// dedicated changes (Add/RemoveVariantType) so concurrent edits merge; patching it by index
/// would silently target whatever happens to sit at that position. Endpoints are
/// delete-and-recreate, and headwords are derived caches.
/// </summary>
public class VariantUpdateValidator : AbstractValidator<UpdateObjectInput<Variant>>
{
    public VariantUpdateValidator()
    {
        RuleFor(u => u.Patch).DoesNotChangePropertyOrChildren(nameof(Variant.Types));
        RuleFor(u => u.Patch).DoesNotChangeProperty(nameof(Variant.VariantEntryId));
        RuleFor(u => u.Patch).DoesNotChangeProperty(nameof(Variant.MainEntryId));
        RuleFor(u => u.Patch).DoesNotChangeProperty(nameof(Variant.MainSenseId));
        RuleFor(u => u.Patch).DoesNotChangeProperty(nameof(Variant.VariantHeadword));
        RuleFor(u => u.Patch).DoesNotChangeProperty(nameof(Variant.MainHeadword));
        RuleFor(u => u.Patch).DoesNotChangeProperty(nameof(Variant.DeletedAt));
        RuleFor(u => u.Patch).DoesNotChangeProperty(nameof(Variant.Id));
        RuleFor(u => u.Patch.Operations).NotEmpty();
    }
}
