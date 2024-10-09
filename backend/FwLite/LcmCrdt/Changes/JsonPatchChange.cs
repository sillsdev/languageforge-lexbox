using System.Text.Json;
using System.Text.Json.Serialization;
using SIL.Harmony.Changes;
using SIL.Harmony.Entities;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Internal;
using SystemTextJsonPatch.Operations;

namespace LcmCrdt.Changes;

public class JsonPatchChange<T> : EditChange<T>, IPolyType where T : class
{
    public static string TypeName => "jsonPatch:" + typeof(T).Name;
    public JsonPatchChange(Guid entityId, Action<JsonPatchDocument<T>> action) : base(entityId)
    {
        PatchDocument = new();
        action(PatchDocument);
        JsonPatchValidator.ValidatePatchDocument(PatchDocument);
    }

    [JsonConstructor]
    public JsonPatchChange(Guid entityId, JsonPatchDocument<T> patchDocument): base(entityId)
    {
        PatchDocument = patchDocument;
        JsonPatchValidator.ValidatePatchDocument(PatchDocument);
    }


    public JsonPatchDocument<T> PatchDocument { get; }

    public override ValueTask ApplyChange(T entity, ChangeContext context)
    {
        PatchDocument.ApplyTo(entity);
        return ValueTask.CompletedTask;
    }
}

file static class JsonPatchValidator
{

    /// <summary>
    /// prevents the use of indexes in the path, as this will cause major problems with CRDTs.
    /// </summary>
    public static void ValidatePatchDocument(IJsonPatchDocument patchDocument)
    {
        foreach (var operation in patchDocument.GetOperations())
        {
            if (operation.OperationType == OperationType.Remove)
            {
                throw new NotSupportedException("remove at index not supported");
            }

            // we want to make sure that the path is not an index, as a shortcut we just check the first character is not a digit, because it's invalid for fields to start with a digit.
            //however this could be overriden with a json path name
            if (new ParsedPath(operation.Path).Segments.Any(s => char.IsDigit(s[0])))
            {
                throw new NotSupportedException($"no path operation can be made with an index, path: {operation.Path}");
            }
        }
    }
}
