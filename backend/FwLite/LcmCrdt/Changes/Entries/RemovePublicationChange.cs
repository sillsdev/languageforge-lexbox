using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Entries;

public class RemovePublicationChange(Guid entityId, Guid publicationId) : EditChange<Entry>(entityId), ISelfNamedType<RemovePublicationChange>
{
    public Guid PublicationId { get; } = publicationId;
    public override ValueTask ApplyChange(Entry entity, IChangeContext context)
    {
        entity.PublishIn.RemoveAll(t => t.Id == PublicationId);
        return ValueTask.CompletedTask;
    }
}
