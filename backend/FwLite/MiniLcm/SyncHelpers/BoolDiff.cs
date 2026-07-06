using SystemTextJsonPatch.Operations;

namespace MiniLcm.SyncHelpers;

public static class BoolDiff
{
    public static IEnumerable<Operation<T>> GetBoolDiff<T>(string path,
        bool before,
        bool after) where T : class
    {
        if (before == after) yield break;
        yield return new Operation<T>("replace", $"/{path}", null, after);
    }
}
