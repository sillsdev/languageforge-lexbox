using LcmCrdt.Objects;
using MiniLcm.Models;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class CreateVariantType(Guid entityId, MultiString name) : CreateChange<VariantType>(entityId), ISelfNamedType<CreateVariantType>
{
    public MultiString Name { get; } = name;
    public override ValueTask<VariantType> NewEntity(Commit commit, IChangeContext context)
    {
        return ValueTask.FromResult(new VariantType
        {
            Id = EntityId,
            Name = Name
        });
    }
}
