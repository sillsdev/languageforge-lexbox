using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Entries;

public class ReplacePublicationChange(Guid entityId, Publication newPublication, Guid oldPublicationId) : EditChange<Entry>(entityId), ISelfNamedType<ReplacePublicationChange>
{
    public Publication NewPublication { get; } = newPublication;
    public Guid OldPublicationId { get; } = oldPublicationId;

    public override async ValueTask ApplyChange(Entry entity, IChangeContext context)
    {
        entity.PublishIn.RemoveAll(t => t.Id == OldPublicationId);
        if (entity.PublishIn.Any(t => t.Id == NewPublication.Id)) return;
        if (await context.IsObjectDeleted(NewPublication.Id)) return;
        entity.PublishIn.Add(NewPublication);
    }
}
