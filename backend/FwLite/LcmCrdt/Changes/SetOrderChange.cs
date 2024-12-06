using System.Text.Json.Serialization;
using SIL.Harmony.Changes;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class SetOrderChange<T> : EditChange<T>, ISelfNamedType<SetOrderChange<T>>
    where T : class, IOrderable
{
    public static IChange Between(Guid entityId, T left, T right)
    {
        return new SetOrderChange<T>(entityId, (left.Order + right.Order) / 2);
    }

    public static IChange After(Guid entityId, T previous)
    {
        return new SetOrderChange<T>(entityId, previous.Order + 1);
    }

    public static IChange Before(Guid entityId, T preceding)
    {
        return new SetOrderChange<T>(entityId, preceding.Order - 1);
    }

    public static IChange To(Guid entityId, double order)
    {
        return new SetOrderChange<T>(entityId, order);
    }

    [JsonConstructor]
    protected SetOrderChange(Guid entityId, double order) : base(entityId)
    {
        Order = order;
    }

    public double Order { get; init; }

    public override ValueTask ApplyChange(T entity, ChangeContext context)
    {
        entity.Order = Order;
        return ValueTask.CompletedTask;
    }
}
