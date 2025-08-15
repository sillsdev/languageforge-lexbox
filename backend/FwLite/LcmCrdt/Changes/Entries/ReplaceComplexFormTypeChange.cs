using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Entries;

public class ReplaceComplexFormTypeChange(Guid entityId, ComplexFormType newComplexFormType, Guid oldComplexFormTypeId) : EditChange<Entry>(entityId), ISelfNamedType<ReplaceComplexFormTypeChange>
{
    public ComplexFormType NewComplexFormType { get; } = newComplexFormType;
    public Guid OldComplexFormTypeId { get; } = oldComplexFormTypeId;

    public override async ValueTask ApplyChange(Entry entity, IChangeContext context)
    {
        entity.ComplexFormTypes.RemoveAll(t => t.Id == OldComplexFormTypeId);
        if (entity.ComplexFormTypes.Any(t => t.Id == NewComplexFormType.Id)) return;
        if (await context.IsObjectDeleted(NewComplexFormType.Id)) return;
        entity.ComplexFormTypes.Add(NewComplexFormType);
    }
}
