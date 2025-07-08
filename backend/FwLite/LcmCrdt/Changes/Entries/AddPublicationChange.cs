using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Entries;

public class AddPublicationChange(Guid entityId, Publication publication)
    : EditChange<Entry>(entityId), ISelfNamedType<AddPublicationChange>
{
    public Publication Publication { get; } = publication;

    public override async ValueTask ApplyChange(Entry entity, IChangeContext context)
    {
        if (entity.PublishIn.Any(t => t.Id == Publication.Id)) return;
        if (await context.IsObjectDeleted(Publication.Id)) return;
        entity.PublishIn.Add(Publication);
    }
}
