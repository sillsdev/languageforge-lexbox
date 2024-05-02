using Crdt.Core;
using CrdtLib.Changes;
using CrdtLib.Db;
using CrdtLib.Entities;
using CrdtSample.Models;

namespace CrdtSample.Changes;

public class AddAntonymReferenceChange(Guid entityId, Guid antonymId)
    : Change<Word>(entityId), ISelfNamedType<AddAntonymReferenceChange>
{
    public Guid AntonymId { get; set; } = antonymId;

    public override IObjectBase NewEntity(Commit commit)
    {
        throw new NotImplementedException();
    }

    public override async ValueTask ApplyChange(Word entity, ChangeContext context)
    {
        if (!await context.IsObjectDeleted(AntonymId))
            entity.AntonymId = AntonymId;
    }
}