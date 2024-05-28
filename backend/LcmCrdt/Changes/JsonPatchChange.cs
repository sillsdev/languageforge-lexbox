using System.Text.Json;
using System.Text.Json.Serialization;
using Crdt;
using Crdt.Changes;
using Crdt.Db;
using Crdt.Entities;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace LcmCrdt.Changes;

public class JsonPatchChange<T> : EditChange<T>, IPolyType where T : class, IPolyType, IObjectBase
{
    public static string TypeName => "jsonPatch:" + T.TypeName;
    public JsonPatchChange(Guid entityId, Action<JsonPatchDocument<T>> action) : base(entityId)
    {
        PatchDocument = new();
        action(PatchDocument);
    }

    [JsonConstructor]
    public JsonPatchChange(Guid entityId, JsonPatchDocument<T> patchDocument): base(entityId)
    {
        PatchDocument = patchDocument;
    }
    public JsonPatchChange(Guid entityId, IJsonPatchDocument patchDocument, JsonSerializerOptions options): base(entityId)
    {
        PatchDocument = new JsonPatchDocument<T>(patchDocument.GetOperations().Select(o =>
            new Operation<T>(o.Op!, o.Path!, o.From, o.Value)).ToList(), options);
    }

    public JsonPatchDocument<T> PatchDocument { get; }

    public override ValueTask ApplyChange(T entity, ChangeContext context)
    {
        PatchDocument.ApplyTo(entity);
        return ValueTask.CompletedTask;
    }
}
