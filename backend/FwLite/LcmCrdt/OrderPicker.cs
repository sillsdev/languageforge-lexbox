
using MiniLcm.SyncHelpers;
using SIL.Harmony;

namespace LcmCrdt;

public static class OrderPicker
{
    public static async Task<double> PickOrder<T>(this DataModel dataModel, BetweenPosition? between = null, IQueryable<T>? siblings = null) where T : class, IOrderable
    {
        var beforeId = between?.Before;
        var afterId = between?.After;
        var before = beforeId is not null ?
            await dataModel.GetLatest<T>(beforeId.Value) ?? throw new NullReferenceException($"unable to find {typeof(T).Name} with id {beforeId}")
            : null;
        var after = afterId is not null ?
            await dataModel.GetLatest<T>(afterId.Value) ?? throw new NullReferenceException($"unable to find {typeof(T).Name} with id {afterId}")
            : null;

        return (before, after) switch
        {
            (null, null) => siblings?.Select(s => s.Order).DefaultIfEmpty().Max(order => order) + 1 ?? 1,
            (_, null) => before.Order + 1,
            (null, _) => after.Order - 1,
            _ => (before.Order + after.Order) / 2,
        };
    }
}
