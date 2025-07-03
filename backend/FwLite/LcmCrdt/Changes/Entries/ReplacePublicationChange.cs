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
        entity.PublishIn =
        [
            ..entity.PublishIn.Where(t => t.Id != OldPublicationId),
            NewPublication
        ];
        return ValueTask.CompletedTask;
    }
}
