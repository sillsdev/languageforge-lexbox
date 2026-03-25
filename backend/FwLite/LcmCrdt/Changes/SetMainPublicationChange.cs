using LcmCrdt.Utils;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class SetMainPublicationChange(Guid entityId) : EditChange<Publication>(entityId), ISelfNamedType<SetMainPublicationChange>
{
    public override async ValueTask ApplyChange(Publication entity, IChangeContext context)
    {
        // No-op if there is already a main publication (only allow transitioning from 0 to 1)
        var hasMain = await context.GetObjectsOfType<Publication>().AnyAsync(p => p.IsMain);
        if (hasMain) return;

        entity.IsMain = true;
    }
}
