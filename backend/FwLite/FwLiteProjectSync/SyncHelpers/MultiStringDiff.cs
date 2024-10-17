using MiniLcm.Models;
using SystemTextJsonPatch.Operations;

namespace FwLiteProjectSync.SyncHelpers;

public static class MultiStringDiff
{
    public static IEnumerable<Operation<T>> GetMultiStringDiff<T>(string path,
        MultiString previous,
        MultiString current) where T : class
    {
        var currentKeys = current.Values.Keys.ToHashSet();
        foreach (var (key, previousValue) in previous.Values)
        {
            if (current.Values.TryGetValue(key, out var currentValue))
            {
                if (!previousValue.Equals(currentValue))
                    yield return new Operation<T>("replace", $"/{path}/{key}", null, currentValue);
            }
            else
            {
                yield return new Operation<T>("remove", $"/{path}/{key}", null);
            }

            currentKeys.Remove(key);
        }

        foreach (var key in currentKeys)
        {
            yield return new Operation<T>("add", $"/{path}/{key}", null, current.Values[key]);
        }
    }
}
