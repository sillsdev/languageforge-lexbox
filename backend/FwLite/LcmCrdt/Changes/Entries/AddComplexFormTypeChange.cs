using MiniLcm.Models;
using SIL.Harmony.Changes;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Entries;

public class AddComplexFormTypeChange(Guid entityId, ComplexFormType complexFormType)
    : EditChange<Entry>(entityId), ISelfNamedType<AddComplexFormTypeChange>
{
    public ComplexFormType ComplexFormType { get; } = complexFormType;

    public override async ValueTask ApplyChange(Entry entity, ChangeContext context)
    {
        if (await context.IsObjectDeleted(ComplexFormType.Id)) return;
        entity.ComplexFormTypes.Add(ComplexFormType);
    }
}
