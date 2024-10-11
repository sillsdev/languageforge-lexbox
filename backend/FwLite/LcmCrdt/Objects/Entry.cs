﻿
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Utils;
using SIL.Harmony;
using SIL.Harmony.Entities;
using LinqToDB;
using MiniLcm.Models;
using SIL.Harmony.Changes;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace LcmCrdt.Objects;

public class Entry : MiniLcm.Models.Entry, IObjectBase<Entry>
{
    Guid IObjectBase.Id
    {
        get => Id;
        init => Id = value;
    }

    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// This is a bit of a hack, we want to be able to reference senses when running a query, and they must be CrdtSenses
    /// however we only want to store the senses in the entry as MiniLcmSenses, so we need to convert them back to CrdtSenses
    /// Note, even though this is JsonIgnored, the Senses property in the base class is still serialized
    /// </summary>
    [JsonIgnore]
    public new IReadOnlyList<Sense> Senses
    {
        get
        {
            return [..base.Senses.Select(s => s as Sense ?? Sense.FromMiniLcm(s, Id))];
    }
        set { base.Senses = [..value]; }
    }


    [ExpressionMethod(nameof(HeadwordExpression))]
    public string Headword(WritingSystemId ws)
    {
        var word = CitationForm[ws];
        if (string.IsNullOrEmpty(word)) word = LexemeForm[ws];
        return word.Trim();
    }

    protected static Expression<Func<Entry, WritingSystemId, string?>> HeadwordExpression() =>
        (e, ws) => (string.IsNullOrEmpty(Json.Value(e.CitationForm, ms => ms[ws]))
            ? Json.Value(e.LexemeForm, ms => ms[ws])
            : Json.Value(e.CitationForm, ms => ms[ws]))!.Trim();

    public Guid[] GetReferences()
    {
        return
        [
            ..Components.SelectMany(c =>
                c.ComponentSenseId is null
                    ? [c.ComponentEntryId]
                    : new[] { c.ComponentEntryId, c.ComponentSenseId.Value }),
            ..ComplexForms.Select(c => c.ComplexFormEntryId)
        ];
    }

    public void RemoveReference(Guid id, Commit commit)
    {
        Components = Components.Where(c => c.ComponentEntryId != id && c.ComponentSenseId != id).ToList();
        ComplexForms = ComplexForms.Where(c => c.ComplexFormEntryId != id).ToList();
    }

    public IObjectBase Copy()
    {
        return new Entry
        {
            Id = Id,
            DeletedAt = DeletedAt,
            LexemeForm = LexemeForm.Copy(),
            CitationForm = CitationForm.Copy(),
            LiteralMeaning = LiteralMeaning.Copy(),
            Note = Note.Copy(),
            Senses = [..Senses.Select(s => (Sense)s.Copy())],
            Components =
            [
                ..Components.Select(c => (c is CrdtComplexFormComponent cc ? (ComplexFormComponent)cc.Copy() : c))
            ],
            ComplexForms =
            [
                ..ComplexForms.Select(c => (c is CrdtComplexFormComponent cc ? (ComplexFormComponent)cc.Copy() : c))
            ],
            ComplexFormTypes =
            [
                ..ComplexFormTypes.Select(cft => (cft is CrdtComplexFormType ct ? (ComplexFormType)ct.Copy() : cft))
            ]
        };
    }

    public static IEnumerable<IChange> ChangesFromJsonPatch(Entry entry, JsonPatchDocument<MiniLcm.Models.Entry> patch)
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
                return new DeleteChange<CrdtComplexFormComponent>(component.Id);
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
            yield return new JsonPatchChange<Entry>(entry.Id, patch, patch.Options);
    }
}