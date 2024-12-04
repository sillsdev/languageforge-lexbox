
using SIL.Harmony;

namespace LcmCrdt;

public static class OrderPicker
{
    public static async Task<double> PickOrder<T>(this DataModel dataModel, Guid? afterId = null, Guid? beforeId = null) where T : class, IOrderable
    {
        var after = afterId is not null ?
            await dataModel.GetLatest<T>(afterId.Value) ?? throw new NullReferenceException($"unable to find {typeof(T).Name} with id {afterId}")
            : null;
        var before = beforeId is not null ?
            await dataModel.GetLatest<T>(beforeId.Value) ?? throw new NullReferenceException($"unable to find {typeof(T).Name} with id {beforeId}")
            : null;

        return (after, before) switch
        {
            (null, null) => 1,
            (null, _) => before.Order - 1,
            (_, null) => after.Order + 1,
            _ => (before.Order + after.Order) / 2,
        };
    }
}
