using SIL.Harmony.Changes;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Entries;

public class RemoveComplexFormTypeChange(Guid entityId, Guid complexFormId) : EditChange<Entry>(entityId), ISelfNamedType<RemoveComplexFormTypeChange>
{
    public Guid ComplexFormId { get; } = complexFormId;
    public override ValueTask ApplyChange(Entry entity, ChangeContext context)
    {
        entity.ComplexFormTypes = entity.ComplexFormTypes.Where(t => t.Id != ComplexFormId).ToList();
        return ValueTask.CompletedTask;
    }
}
