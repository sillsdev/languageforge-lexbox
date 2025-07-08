using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Entries;

public class ReplacePublicationChange(Guid entityId, Publication newPublication, Guid oldPublicationId) : EditChange<Entry>(entityId), ISelfNamedType<ReplacePublicationChange>
{
    public Publication NewPublication { get; } = newPublication;
    public Guid OldPublicationId { get; } = oldPublicationId;

    public override ValueTask ApplyChange(Entry entity, IChangeContext context)
    {
        if (entity.PublishIn.Any(t => t.Id == NewPublication.Id))
        {
            // Just remove old one, don't add new as it would be a duplicate
            entity.PublishIn.RemoveAll(t => t.Id == OldPublicationId);
        }
        else
        {
            // More efficient to add and remove in one step
            entity.PublishIn =
            [
                ..entity.PublishIn.Where(t => t.Id != OldPublicationId),
                NewPublication
            ];
        }
        return ValueTask.CompletedTask;
    }
}
