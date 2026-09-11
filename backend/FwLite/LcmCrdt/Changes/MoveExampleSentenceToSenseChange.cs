using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

/// <summary>
/// Reparents an example sentence to a different sense, which can be done in a handful of ways in FieldWorks/LibLCM.
/// </summary>
public class MoveExampleSentenceToSenseChange(Guid entityId, Guid senseId, double order) : EditChange<ExampleSentence>(entityId), ISelfNamedType<MoveExampleSentenceToSenseChange>
{
    public Guid SenseId { get; } = senseId;
    public double Order { get; init; } = order;

    public override async ValueTask ApplyChange(ExampleSentence example, IChangeContext context)
    {
        example.SenseId = SenseId;
        example.Order = Order;
        if (example.DeletedAt == null && await context.IsObjectDeleted(SenseId))
        {
            example.DeletedAt = context.Commit.DateTime;
        }
    }
}
