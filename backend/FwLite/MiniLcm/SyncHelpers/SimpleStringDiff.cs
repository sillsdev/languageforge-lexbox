using SystemTextJsonPatch.Operations;

namespace MiniLcm.SyncHelpers;

public static class SimpleStringDiff
{
    public static IEnumerable<Operation<T>> GetStringDiff<T>(string path,
        string? before,
        string? after) where T : class
    {
        if (before == after) yield break;
        if (after is null) yield return new Operation<T>("remove", $"/{path}", null);
        else if (before is null) yield return new Operation<T>("add", $"/{path}", null, after);
        else yield return new Operation<T>("replace", $"/{path}", null, after);
    }
}
