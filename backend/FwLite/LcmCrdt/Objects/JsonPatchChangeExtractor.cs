using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Utils;
using SIL.Harmony.Changes;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace LcmCrdt.Objects;

public static class JsonPatchChangeExtractor
{
    public static IEnumerable<IChange> ToChanges(this Sense sense, JsonPatchDocument<Sense> patch)
    {
        //the part of speech text should not be changed directly, instead we should set the part of speech id
        patch.RemoveChanges(s => s.PartOfSpeech);
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


    public static IEnumerable<IChange> ToChanges(this Entry entry, JsonPatchDocument<Entry> patch)
    {
        IChange RewriteComplexFormComponents(IList<ComplexFormComponent> components, ComplexFormComponent? component, Index index, OperationType operationType)
        {
            if (operationType == OperationType.Add)
            {
                ArgumentNullException.ThrowIfNull(component);
                return new AddEntryComponentChange(component);
            }

            if (operationType == OperationType.Replace)
            {
                ArgumentNullException.ThrowIfNull(component);
                var currentComponent = components[index];
                if (currentComponent.ComponentEntryId != component.ComponentEntryId && currentComponent.ComplexFormEntryId != component.ComplexFormEntryId)
                {
                    throw new InvalidOperationException("both component id and complex form id have changed");
                }

                if (currentComponent.Id != component.Id) throw new InvalidOperationException($"complexFormComponent id mismatch at index {index}, expected {currentComponent.Id}, actual {component.Id}");
                if (currentComponent.ComponentEntryId != component.ComponentEntryId)
                {
                    return SetComplexFormComponentChange.NewComponent(currentComponent.Id, component.ComponentEntryId);
                }

                if (currentComponent.ComponentSenseId != component.ComponentSenseId)
                {
                    return SetComplexFormComponentChange.NewComponentSense(currentComponent.Id, component.ComponentEntryId, component.ComponentSenseId);
                }

                if (currentComponent.ComplexFormEntryId != component.ComplexFormEntryId)
                {
                    return SetComplexFormComponentChange.NewComplexForm(currentComponent.Id, component.ComplexFormEntryId);
                }
            }

            if (operationType == OperationType.Remove)
            {
                component ??= components[index];
                return new DeleteChange<ComplexFormComponent>(component.Id);
            }

            throw new NotSupportedException($"operation {operationType} not supported for components");
        }

        foreach (var rewriteChange in patch.RewriteChanges(
                     s => s.Components,
                     (component, index, operationType) => RewriteComplexFormComponents(entry.Components, component, index, operationType)
                 ))
        {
            yield return rewriteChange;
        }

        foreach (var rewriteChange in patch.RewriteChanges(
                     s => s.ComplexForms,
                     (component, index, operationType) => RewriteComplexFormComponents(entry.ComplexForms, component, index, operationType)
                 ))
        {
            yield return rewriteChange;
        }

        foreach (var rewriteChange in patch.RewriteChanges(
                     s => s.ComplexFormTypes,
                     (complexFormType, index, operationType) =>
                     {
                         if (operationType == OperationType.Add)
                         {
                             ArgumentNullException.ThrowIfNull(complexFormType);
                             return new AddComplexFormTypeChange(entry.Id, complexFormType);
                         }

                         if (operationType == OperationType.Remove)
                         {
                             complexFormType ??= entry.ComplexFormTypes[index];
                             return new RemoveComplexFormTypeChange(entry.Id, complexFormType.Id);
                         }

                         if (operationType == OperationType.Replace)
                         {
                             ArgumentNullException.ThrowIfNull(complexFormType);
                             var currentComplexFormType = entry.ComplexFormTypes[index];
                             return new ReplaceComplexFormTypeChange(entry.Id, complexFormType, currentComplexFormType.Id);
                         }
                         throw new NotSupportedException($"operation {operationType} not supported for complex form types");
                     }))
        {
            yield return rewriteChange;
        }



        if (patch.Operations.Count > 0)
            yield return new JsonPatchChange<Entry>(entry.Id, patch);
    }
}
