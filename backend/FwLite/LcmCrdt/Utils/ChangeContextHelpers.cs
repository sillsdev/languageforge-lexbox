using SIL.Harmony.Changes;

namespace LcmCrdt.Utils;

public static class ChangeContextHelpers
{
    public static async ValueTask<bool> IsObjectDeleted(this ChangeContext context, Guid? entityId)
    {
        return entityId is null || await context.IsObjectDeleted(entityId.Value);
    }

    public static async ValueTask<Guid?> DeletedAsNull(this ChangeContext context, Guid? entityId)
    {
        if (entityId is null) return null;
        return await context.IsObjectDeleted(entityId.Value) ? null : entityId;
    }

    public static async IAsyncEnumerable<T> FilterDeleted<T>(this ChangeContext context, IEnumerable<T> entities)
        where T : IObjectWithId
    {
        foreach (var objectWithId in entities)
        {
            if (await context.IsObjectDeleted(objectWithId.Id)) continue;
            yield return objectWithId;
        }
    }
}
