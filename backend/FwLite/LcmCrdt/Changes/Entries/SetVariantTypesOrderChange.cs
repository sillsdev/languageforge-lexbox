using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Entries;

/// <summary>
/// Reorders a variant link's Types to the given id sequence (FLEx: right-click a type →
/// move left/right). Types not listed — e.g. added concurrently — keep their relative
/// order after the listed ones, so this merges with concurrent Add/RemoveVariantTypeChange
/// instead of clobbering them.
/// </summary>
public class SetVariantTypesOrderChange(Guid entityId, List<Guid> orderedTypeIds)
    : EditChange<Variant>(entityId), ISelfNamedType<SetVariantTypesOrderChange>
{
    public List<Guid> OrderedTypeIds { get; } = orderedTypeIds;

    public override ValueTask ApplyChange(Variant entity, IChangeContext context)
    {
        entity.Types = [.. entity.Types
            .Select((type, index) => (type, index))
            .OrderBy(t => OrderedTypeIds.IndexOf(t.type.Id) is var i and >= 0 ? i : int.MaxValue)
            .ThenBy(t => t.index)
            .Select(t => t.type)];
        return ValueTask.CompletedTask;
    }
}
