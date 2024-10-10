using MiniLcm.Models;
using SIL.Harmony.Changes;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Entries;

public class ReplaceComplexFormTypeChange(Guid entityId, ComplexFormType newComplexFormType, Guid oldComplexFormTypeId) : EditChange<Entry>(entityId), ISelfNamedType<ReplaceComplexFormTypeChange>
{
    public ComplexFormType NewComplexFormType { get; } = newComplexFormType;
    public Guid OldComplexFormTypeId { get; } = oldComplexFormTypeId;

    public override ValueTask ApplyChange(Entry entity, ChangeContext context)
    {
        entity.ComplexFormTypes =
        [
            ..entity.ComplexFormTypes.Where(t => t.Id != OldComplexFormTypeId),
            NewComplexFormType
        ];
        return ValueTask.CompletedTask;
    }
}
