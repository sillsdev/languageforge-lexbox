using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace LcmCrdt.Changes.CustomJsonPatches;

public static class JsonPatchHelpers
{
    public static void RewritePaths<T>(this JsonPatchDocument<T> patchDocument,
        PathMatchType matchType,
        string path,
        Func<Operation<T>, IEnumerable<Operation<T>>> rewrite,
        bool removeOriginal = true) where T : class
    {
        var newOperations = new List<Operation<T>>();
        for (var i = patchDocument.Operations.Count - 1; i >= 0; i--)
        {
            var operation = patchDocument.Operations[i];
            switch (matchType)
            {
                case PathMatchType.StartsWith:
                    if (operation.Path?.StartsWith(path) != true) continue;
                    break;
            }

            if (removeOriginal)
                patchDocument.Operations.RemoveAt(i);
            newOperations.AddRange(rewrite(operation));
        }
        patchDocument.Operations.AddRange(newOperations);
    }
}

public enum PathMatchType
{
    StartsWith
}
