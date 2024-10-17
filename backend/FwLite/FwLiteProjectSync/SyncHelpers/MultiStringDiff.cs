using MiniLcm.Models;
using SystemTextJsonPatch.Operations;

namespace FwLiteProjectSync.SyncHelpers;

public static class MultiStringDiff
{
    public static IEnumerable<Operation<T>> GetMultiStringDiff<T>(string path,
        MultiString before,
        MultiString after) where T : class
    {
        var afterKeys = after.Values.Keys.ToHashSet();
        foreach (var (key, beforeValue) in before.Values)
        {
            if (after.Values.TryGetValue(key, out var afterValue))
            {
                if (!beforeValue.Equals(afterValue))
                    yield return new Operation<T>("replace", $"/{path}/{key}", null, afterValue);
            }
            else
            {
                yield return new Operation<T>("remove", $"/{path}/{key}", null);
            }

            afterKeys.Remove(key);
        }

        foreach (var key in afterKeys)
        {
            yield return new Operation<T>("add", $"/{path}/{key}", null, after.Values[key]);
        }
    }
}
