using LcmCrdt.Objects;
using SIL.Harmony.Changes;
using SIL.Harmony.Entities;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace LcmCrdt.Changes.CustomJsonPatches;

public class JsonPatchExampleSentenceChange : JsonPatchChange<ExampleSentence>
{
    public JsonPatchExampleSentenceChange(Guid entityId, JsonPatchDocument<ExampleSentence> patchDocument) : base(entityId, patchDocument, bypassValidation: true)
    {
        patchDocument.RewritePaths(PathMatchType.StartsWith, "/Translation/", Rewrite);

        static IEnumerable<Operation<ExampleSentence>> Rewrite(Operation<ExampleSentence> operation)
        {
            var newOp = new Operation<ExampleSentence>();
            newOp.Op = operation.Op;
            newOp.Path = operation.Path?.Replace("/Translation/", "/Translations/0/Text/");
            newOp.Value = operation.Value;
            if (operation.OperationType == OperationType.Add)
            {
                yield return new Operation<ExampleSentence>("add", "/Translations/0", null, new Translation());
            }

            yield return newOp;
        }

        JsonPatchValidator.ValidatePatchDocument(patchDocument, operation => operation.Path?.StartsWith("/Translations/") != true);
    }
}
