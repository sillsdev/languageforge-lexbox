using System.Text.Json.Serialization;
using Crdt.Core;
using CrdtLib.Changes;
using CrdtLib.Db;
using CrdtLib.Entities;
using CrdtSample.Models;
using Ycs;

namespace CrdtSample.Changes;

public class EditExampleChange : Change<Example>, ISelfNamedType<EditExampleChange>
{
    public static EditExampleChange EditExample(Example example, Action<YText> change)
    {
        var text = example.YText;
        var stateBefore = text.Doc.EncodeStateVectorV2();
        change(text);
        var updateBlob = Convert.ToBase64String(text.Doc.EncodeStateAsUpdateV2(stateBefore));
        return new EditExampleChange(example.Id, updateBlob);
    }


    [JsonConstructor]
    public EditExampleChange(Guid entityId, string updateBlob) : base(entityId)
    {
        UpdateBlob = updateBlob;
    }

    public override IObjectBase NewEntity(Commit commit)
    {
        throw new System.NotImplementedException();
    }

    public string UpdateBlob { get; set; }

    public override ValueTask ApplyChange(Example entity, ChangeContext context)
    {
        entity.YText.Doc.ApplyUpdateV2(Convert.FromBase64String(UpdateBlob));
        return ValueTask.CompletedTask;
    }
}