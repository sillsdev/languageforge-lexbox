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
        if (await context.IsObjectDeleted(ComplexFormType.Id))
        {
            //do nothing, don't add the type if it's already deleted
        }
        else if (entity.ComplexFormTypes.All(s => s.Id != ComplexFormType.Id))
        {
            //only add if it's not already in the list
            entity.ComplexFormTypes = [..entity.ComplexFormTypes, ComplexFormType];
        }
    }
}
