using LcmCrdt.Objects;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;

namespace LcmCrdt.Utils;

public static class ChangeContextHelpers
{
    public static async ValueTask<bool> IsObjectDeleted(this IChangeContext context, Guid? entityId)
    {
        return entityId is null || await context.IsObjectDeleted(entityId.Value);
    }

    public static async ValueTask<Guid?> DeletedAsNull(this IChangeContext context, Guid? entityId)
    {
        if (entityId is null) return null;
        return await context.IsObjectDeleted(entityId.Value) ? null : entityId;
    }

    public static async IAsyncEnumerable<T> FilterDeleted<T>(this IChangeContext context, IEnumerable<T> entities)
        where T : IObjectWithId
    {
        foreach (var objectWithId in entities)
        {
            if (await context.IsObjectDeleted(objectWithId.Id)) continue;
            yield return objectWithId;
        }
    }

    public static IAsyncEnumerable<T> GetObjectsOfType<T>(this IChangeContext context)
        where T : class, IObjectWithId
    {
        return context.GetObjectsOfType<T>(MiniLcmCrdtAdapter.GetObjectTypeName(typeof(T)));
    }
}
