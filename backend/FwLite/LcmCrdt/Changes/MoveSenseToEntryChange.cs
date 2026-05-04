using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

/// <summary>
/// Reparents a sense to a different entry, which can be done in a handful of ways in FieldWorks/LibLCM.
/// </summary>
public class MoveSenseToEntryChange(Guid entityId, Guid entryId, double order) : EditChange<Sense>(entityId), ISelfNamedType<MoveSenseToEntryChange>
{
    public Guid EntryId { get; } = entryId;
    public double Order { get; init; } = order;

    public override async ValueTask ApplyChange(Sense sense, IChangeContext context)
    {
        sense.EntryId = EntryId;
        sense.Order = Order;
        if (sense.DeletedAt == null && await context.IsObjectDeleted(EntryId))
        {
            sense.DeletedAt = context.Commit.DateTime;
        }
    }
}
