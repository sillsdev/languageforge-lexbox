using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Entries;

public class AddComplexFormTypeChange(Guid entityId, ComplexFormType complexFormType)
    : EditChange<Entry>(entityId), ISelfNamedType<AddComplexFormTypeChange>
{
    public ComplexFormType ComplexFormType { get; } = complexFormType;

    public override async ValueTask ApplyChange(Entry entity, IChangeContext context)
    {
        if (entity.ComplexFormTypes.Any(t => t.Id == ComplexFormType.Id)) return;
        if (await context.IsObjectDeleted(ComplexFormType.Id)) return;
        entity.ComplexFormTypes.Add(ComplexFormType);
    }
}
