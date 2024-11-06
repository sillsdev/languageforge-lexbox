using LcmCrdt.Objects;
using MiniLcm.Models;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class CreateComplexFormType(Guid entityId, MultiString name) : CreateChange<ComplexFormType>(entityId), ISelfNamedType<CreateComplexFormType>
{
    public MultiString Name { get; } = name;
    public override ValueTask<ComplexFormType> NewEntity(Commit commit, ChangeContext context)
    {
        return ValueTask.FromResult(new ComplexFormType
        {
            Id = EntityId,
            Name = Name
        });
    }
}
