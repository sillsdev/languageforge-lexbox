using LcmCrdt.Objects;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Adapters;
using SystemTextJsonPatch.Internal;
using SystemTextJsonPatch.Operations;

namespace LcmCrdt.Changes.CustomJsonPatches;

public class JsonPatchExampleSentenceChange : JsonPatchChange<ExampleSentence>
{
    public JsonPatchExampleSentenceChange(Guid entityId, JsonPatchDocument<ExampleSentence> patchDocument) : base(entityId, patchDocument)
    {
    }

    public override ValueTask ApplyChange(ExampleSentence entity, IChangeContext context)
    {
        var adapter = new ObjectAdapter(PatchDocument.Options, null, new AdapterFactory());
        //we don't want to modify the original document, so rather than removing the operations we just apply them in a loop ourselves skipping the translation ops
        foreach (var op in PatchDocument.Operations)
        {
            if (op.Path?.StartsWith("/Translation/") == true)
            {
                ApplyTranslationOp(op, entity, adapter);
                continue;
            }
            op.Apply(entity, adapter);
        }
        return ValueTask.CompletedTask;
    }

    private static void ApplyTranslationOp(Operation<ExampleSentence> op, ExampleSentence entity, IObjectAdapter adapter)
    {
        var wsId = new ParsedPath(op.Path).LastSegment;
        var richString = op.Value;
        if (!entity.Translations.Any())
        {
            if (op.Op == "remove")
                return; //nothing to remove
            entity.Translations.Add(new Translation() { Id = Guid.NewGuid() });
        }
        var translation = entity.Translations[0];
        var newOp = new Operation(op.Op ?? "replace", "/Text/" + wsId, null, richString);
        newOp.Apply(translation, adapter);
    }
}
