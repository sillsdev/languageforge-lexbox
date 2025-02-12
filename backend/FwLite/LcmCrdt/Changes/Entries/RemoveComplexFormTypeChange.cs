using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Entries;

public class RemoveComplexFormTypeChange(Guid entityId, Guid complexFormTypeId) : EditChange<Entry>(entityId), ISelfNamedType<RemoveComplexFormTypeChange>
{
    public Guid ComplexFormTypeId { get; } = complexFormTypeId;
    public override ValueTask ApplyChange(Entry entity, IChangeContext context)
    {
        entity.ComplexFormTypes = entity.ComplexFormTypes.Where(t => t.Id != ComplexFormTypeId).ToList();
        return ValueTask.CompletedTask;
    }
}
