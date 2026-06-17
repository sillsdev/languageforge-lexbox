using LcmCrdt.Utils;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class CreatePublicationChange(Guid entityId, MultiString name, bool isMain = false) : CreateChange<Publication>(entityId), ISelfNamedType<CreatePublicationChange>
{
    public MultiString Name { get; } = name;
    public bool IsMain { get; } = isMain;

    public override async ValueTask<Publication> NewEntity(Commit commit, IChangeContext context)
    {
        // Two replicas can each create a main publication offline. On merge we keep the single-main invariant by
        // creating the later one as non-main (rather than deleting it — it's still a real publication).
        var mainAlreadyExists = IsMain && await context.GetObjectsOfType<Publication>().AnyAsync(p => p.IsMain);
        return new Publication
        {
            Id = EntityId,
            Name = Name,
            IsMain = IsMain && !mainAlreadyExists,
        };
    }
}
