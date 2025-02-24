using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class CreatePublicationChange(Guid id, MultiString pubName) : CreateChange<Publication>(id), ISelfNamedType<CreatePublicationChange>
{
    public MultiString Name { get; } = pubName;
    public override ValueTask<Publication> NewEntity(Commit commit, IChangeContext context)
    {
        return ValueTask.FromResult(new Publication { Id = EntityId, Name = Name});
    }
}
