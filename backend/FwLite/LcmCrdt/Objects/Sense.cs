using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Db;
using SIL.Harmony.Entities;
using LcmCrdt.Changes;
using LcmCrdt.Utils;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace LcmCrdt.Objects;

public class Sense : MiniLcm.Sense, IObjectBase<Sense>
{
    public static Sense FromMiniLcm(MiniLcm.Sense sense, Guid entryId)
    {
        return new Sense
        {
            Id = sense.Id,
            Definition = sense.Definition,
            Gloss = sense.Gloss,
            PartOfSpeech = sense.PartOfSpeech,
            PartOfSpeechId = sense.PartOfSpeechId,
            SemanticDomains = sense.SemanticDomains,
            ExampleSentences = sense.ExampleSentences,
            EntryId = entryId
        };
    }
    public static IEnumerable<IChange> ChangesFromJsonPatch(Sense sense, JsonPatchDocument<MiniLcm.Sense> patch)
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
            yield return new JsonPatchChange<Sense>(sense.Id, patch, patch.Options);
    }

    Guid IObjectBase.Id
    {
        get => Id;
        init => Id = value;
    }

    public DateTimeOffset? DeletedAt { get; set; }
    public required Guid EntryId { get; set; }

    public Guid[] GetReferences()
    {
        ReadOnlySpan<Guid> pos = PartOfSpeechId.HasValue ? [PartOfSpeechId.Value] : [];
        return [EntryId, ..pos, ..SemanticDomains.Select(sd => sd.Id)];
    }

    public void RemoveReference(Guid id, Commit commit)
    {
        if (id == EntryId)
            DeletedAt = commit.DateTime;
        if (id == PartOfSpeechId)
            PartOfSpeechId = null;
        SemanticDomains = [..SemanticDomains.Where(sd => sd.Id != id)];
    }

    public IObjectBase Copy()
    {
        return new Sense
        {
            Id = Id,
            EntryId = EntryId,
            DeletedAt = DeletedAt,
            Definition = Definition.Copy(),
            Gloss = Gloss.Copy(),
            PartOfSpeech = PartOfSpeech,
            PartOfSpeechId = PartOfSpeechId,
            SemanticDomains = [..SemanticDomains],
            ExampleSentences = [..ExampleSentences]
        };
    }
}
