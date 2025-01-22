using System.Text.Json.Serialization;
using LcmCrdt.Objects;
using MiniLcm.Models;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Entries;

public class AddEntryComponentChange : CreateChange<ComplexFormComponent>, ISelfNamedType<AddEntryComponentChange>
{
    public Guid ComplexFormEntryId { get; }
    public Guid ComponentEntryId { get; }
    public Guid? ComponentSenseId { get; }

    [JsonConstructor]
    public AddEntryComponentChange(Guid entityId,
        Guid complexFormEntryId,
        Guid componentEntryId,
        Guid? componentSenseId = null) : base(entityId)
    {
        ComplexFormEntryId = complexFormEntryId;
        ComponentEntryId = componentEntryId;
        ComponentSenseId = componentSenseId;
    }

    public AddEntryComponentChange(ComplexFormComponent component) : this(component.Id == default ? Guid.NewGuid() : component.Id,
        component.ComplexFormEntryId,
        component.ComponentEntryId,
        component.ComponentSenseId)
    {
    }

    public override async ValueTask<ComplexFormComponent> NewEntity(Commit commit, ChangeContext context)
    {
        var complexFormEntry = await context.GetCurrent<Entry>(ComplexFormEntryId);
        var componentEntry = await context.GetCurrent<Entry>(ComponentEntryId);
        Sense? componentSense = null;
        if (ComponentSenseId is not null)
            componentSense = await context.GetCurrent<Sense>(ComponentSenseId.Value);
        var shouldBeDeleted = (complexFormEntry?.DeletedAt is not null ||
                               componentEntry?.DeletedAt is not null ||
                               (ComponentSenseId.HasValue && componentSense?.DeletedAt is not null));
        var component = new ComplexFormComponent
        {
            Id = EntityId,
            ComplexFormEntryId = ComplexFormEntryId,
            ComplexFormHeadword = complexFormEntry?.Headword(),
            ComponentEntryId = ComponentEntryId,
            ComponentHeadword = componentEntry?.Headword(),
            ComponentSenseId = ComponentSenseId,
            DeletedAt = shouldBeDeleted
                ? commit.DateTime
                : (DateTime?)null,
        };
        if (component.DeletedAt is null && await HasReferenceCycle(component, context)) component.DeletedAt = commit.DateTime;
        return component;
    }

    private static async ValueTask<bool> HasReferenceCycle(ComplexFormComponent parent, ChangeContext context)
    {
        if (parent.ComplexFormEntryId == parent.ComponentEntryId) return true;
        //used to avoid checking the same ComplexFormComponent multiple times
        HashSet<Guid> visited = [parent.Id];
        Queue<ComplexFormComponent> queue = new Queue<ComplexFormComponent>();
        queue.Enqueue(parent);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current.ComplexFormEntryId == parent.ComponentEntryId) return true;
            await foreach (var o in context.GetObjectsReferencing(current.ComplexFormEntryId))
            {
                if (o is not ComplexFormComponent cfc) continue;
                if (visited.Contains(cfc.Id)) continue;
                if (cfc.ComplexFormEntryId == parent.ComponentEntryId) return true;
                queue.Enqueue(cfc);
                visited.Add(cfc.Id);
            }
        }
        return false;
    }
}
