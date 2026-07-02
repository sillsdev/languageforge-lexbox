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
        return new Publication
        {
            Id = EntityId,
            Name = Name,
            IsMain = IsMain && !await MainAlreadyExists(context),
        };
    }

    private ValueTask<bool> MainAlreadyExists(IChangeContext context)
    {
        return context.GetObjectsOfType<Publication>().AnyAsync(p => p.IsMain);
    }
}
