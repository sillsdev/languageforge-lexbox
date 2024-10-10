using LcmCrdt.Changes;
using LcmCrdt.Utils;
using SIL.Harmony.Changes;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace LcmCrdt.Objects;

public static class JsonPatchChangeExtractor
{
    public static IEnumerable<IChange> ToChanges(this Sense sense, JsonPatchDocument<Sense> patch)
    {
        foreach (var rewriteChange in patch.RewriteChanges(s => s.PartOfSpeechId,
                     (partOfSpeechId, operationType) =>
                     {
                         if (operationType == OperationType.Replace)
                            return new SetPartOfSpeechChange(sense.Id, partOfSpeechId);
                         throw new NotSupportedException($"operation {operationType} not supported for part of speech");
                     }))
        {
            yield return rewriteChange;
        }

        foreach (var rewriteChange in patch.RewriteChanges(s => s.SemanticDomains,
                     (semanticDomain, index, operationType) =>
                     {
                         if (operationType is OperationType.Add)
                         {
                             ArgumentNullException.ThrowIfNull(semanticDomain);
                             return new AddSemanticDomainChange(semanticDomain, sense.Id);
                         }

                         if (operationType is OperationType.Replace)
                         {
                             ArgumentNullException.ThrowIfNull(semanticDomain);
                             return new ReplaceSemanticDomainChange(sense.SemanticDomains[index].Id, semanticDomain, sense.Id);
                         }
                         if (operationType is OperationType.Remove)
                         {
                             return new RemoveSemanticDomainChange(sense.SemanticDomains[index].Id, sense.Id);
                         }

                         throw new NotSupportedException($"operation {operationType} not supported for semantic domains");
                     }))
        {
            yield return rewriteChange;
        }

        if (patch.Operations.Count > 0)
            yield return new JsonPatchChange<Sense>(sense.Id, patch);
    }
}
