using SIL.Harmony.Changes;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class SetOrderChange<T>(Guid entityId, double order) : EditChange<T>(entityId), ISelfNamedType<SetOrderChange<T>>
    where T : class, IOrderable
{
    public double Order { get; init; } = order;

    public override ValueTask ApplyChange(T entity, ChangeContext context)
    {
        entity.Order = Order;
        return ValueTask.CompletedTask;
    }
}
