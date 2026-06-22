using SystemTextJsonPatch.Operations;

namespace MiniLcm.SyncHelpers;

public static class DoubleDiff
{
    public static IEnumerable<Operation<T>> GetDoubleDiff<T>(string path,
        double? before,
        double? after) where T : class
    {
        if (before == after) yield break;
        if (after is null) yield return new Operation<T>("remove", $"/{path}", null);
        else if (before is null) yield return new Operation<T>("add", $"/{path}", null, after);
        else yield return new Operation<T>("replace", $"/{path}", null, after);
    }
}
