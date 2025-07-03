using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Entries;

public class RemovePublicationChange(Guid entityId, Guid publicationId) : EditChange<Entry>(entityId), ISelfNamedType<RemovePublicationChange>
{
    public Guid PublicationId { get; } = publicationId;
    public override ValueTask ApplyChange(Entry entity, IChangeContext context)
    {
        entity.PublishIn = entity.PublishIn.Where(t => t.Id != PublicationId).ToList();
        return ValueTask.CompletedTask;
    }
}
