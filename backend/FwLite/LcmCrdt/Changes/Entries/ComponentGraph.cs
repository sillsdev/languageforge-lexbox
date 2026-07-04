using SIL.Harmony.Changes;
using SIL.Harmony.Core;

namespace LcmCrdt.Changes.Entries;

internal static class ComponentGraph
{
    /// <summary>
    /// Mirrors LCM's acyclicity rule (LexEntryRef.ValidateAddObjectInternal → LexEntry.AllComponents):
    /// walk everything <paramref name="fromEntryId"/> depends on through BOTH link types
    /// (variant → main, complex form → component); a proposed link is a cycle FLEx rejects
    /// iff <paramref name="forbiddenEntryId"/> is reachable. AllComponents resolves a
    /// sense-targeted component to its owning entry but does NOT recurse into it, so
    /// sense-targeted links are compared against the target without being walked past.
    /// </summary>
    public static async ValueTask<bool> CanReach(Guid fromEntryId, Guid forbiddenEntryId, IChangeContext context)
    {
        HashSet<Guid> visited = [];
        Queue<Guid> queue = new();
        if (fromEntryId == forbiddenEntryId) return true;
        queue.Enqueue(fromEntryId);
        while (queue.Count > 0)
        {
            var entryId = queue.Dequeue();
            if (!visited.Add(entryId)) continue;
            await foreach (var o in context.GetObjectsReferencing(entryId))
            {
                Guid targetEntryId;
                bool targetIsSense;
                switch (o)
                {
                    case Variant v when v.DeletedAt is null && v.VariantEntryId == entryId:
                        targetEntryId = v.MainEntryId;
                        targetIsSense = v.MainSenseId is not null;
                        break;
                    case ComplexFormComponent cfc when cfc.DeletedAt is null && cfc.ComplexFormEntryId == entryId:
                        targetEntryId = cfc.ComponentEntryId;
                        targetIsSense = cfc.ComponentSenseId is not null;
                        break;
                    default:
                        continue;
                }
                if (targetEntryId == forbiddenEntryId) return true;
                if (!targetIsSense) queue.Enqueue(targetEntryId);
            }
        }
        return false;
    }
}
