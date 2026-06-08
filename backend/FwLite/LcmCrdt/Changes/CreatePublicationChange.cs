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
        var mainAlreadyExists = IsMain && await context.GetObjectsOfType<Publication>().AnyAsync(p => p.IsMain);
        // A merged-in second main would break the single-main invariant; like CreateMorphTypeChange, return a
        // pre-deleted object so Harmony filters it out before saving — converging to one main without throwing.
        return new Publication
        {
            Id = EntityId,
            Name = Name,
            IsMain = IsMain,
            DeletedAt = mainAlreadyExists ? commit.DateTime : null
        };
    }
}
