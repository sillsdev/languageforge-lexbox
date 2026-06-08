using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class SetDefaultPublicationChange(Guid entityId) : EditChange<Publication>(entityId), ISelfNamedType<SetDefaultPublicationChange>
{
    public override ValueTask ApplyChange(Publication entity, IChangeContext context)
    {
        entity.DefaultedAt = context.Commit.DateTime;
        return ValueTask.CompletedTask;
    }
}
