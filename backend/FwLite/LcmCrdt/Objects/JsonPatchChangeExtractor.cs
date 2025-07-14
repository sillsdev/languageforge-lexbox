using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Utils;
using SIL.Harmony.Changes;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace LcmCrdt.Objects;

public static class JsonPatchChangeExtractor
{
    public static IEnumerable<IChange> ToChanges<T>(this JsonPatchDocument<T> patch, Guid entityId) where T : class
    {
        if (patch.Operations.Count > 0)
            yield return new JsonPatchChange<T>(entityId, patch);
    }
}
