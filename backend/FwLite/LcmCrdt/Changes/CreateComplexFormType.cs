using LcmCrdt.Objects;
using MiniLcm.Models;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class CreateComplexFormType(Guid entityId, MultiString name) : CreateChange<CrdtComplexFormType>(entityId), ISelfNamedType<CreateComplexFormType>
{
    public MultiString Name { get; } = name;
    public override ValueTask<IObjectBase> NewEntity(Commit commit, ChangeContext context)
    {
        return ValueTask.FromResult<IObjectBase>(new CrdtComplexFormType
        {
            Id = EntityId,
            Name = Name
        });
    }
}
