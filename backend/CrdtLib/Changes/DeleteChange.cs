using System.Diagnostics.CodeAnalysis;
using CrdtLib.Db;
using CrdtLib.Entities;

namespace CrdtLib.Changes;

public class DeleteChange<T>(Guid entityId) : Change<T>(entityId), IPolyType
    where T : IPolyType, IObjectBase
{
    public static string TypeName => "delete:" + T.TypeName;

    public override ValueTask ApplyChange(T entity, ChangeContext context)
    {
        entity.DeletedAt = context.Commit.DateTime;
        return ValueTask.CompletedTask;
    }

    public override IObjectBase NewEntity(Commit commit)
    {
        throw new NotImplementedException();
    }
}