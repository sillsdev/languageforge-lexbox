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
        return patch.ToChanges(sense.Id);
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

    public static IEnumerable<IChange> ToChanges<T>(this JsonPatchDocument<T> patch, Guid entityId) where T : class
    {
        if (patch.Operations.Count > 0)
            yield return new JsonPatchChange<T>(entityId, patch);
    }
}
