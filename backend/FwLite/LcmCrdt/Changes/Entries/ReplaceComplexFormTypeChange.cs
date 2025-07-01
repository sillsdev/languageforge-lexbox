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
        entity.ComplexFormTypes = [..entity.ComplexFormTypes.Where(s => s.Id != OldComplexFormTypeId)];
        if (await context.IsObjectDeleted(NewComplexFormType.Id))
        {
            //do nothing, don't add the type if it's already deleted
        }
        else if (entity.ComplexFormTypes.All(s => s.Id != NewComplexFormType.Id))
        {
            //only add if it's not already in the list
            entity.ComplexFormTypes = [..entity.ComplexFormTypes, NewComplexFormType];
        }
    }
}
