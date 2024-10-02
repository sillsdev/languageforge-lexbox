﻿using LcmCrdt.Changes.Entries;
using LcmCrdt.Objects;
using MiniLcm.Models;
using SIL.Harmony.Changes;
using SystemTextJsonPatch;
using Entry = LcmCrdt.Objects.Entry;

namespace LcmCrdt.Tests;

public class JsonPatchEntryRewriteTests
{
    private Entry _entry = new Entry() { Id = Guid.NewGuid(), LexemeForm = { { "en", "test" } } };

    [Fact]
    public void ChangesFromJsonPatch_AddComponentMakesAddEntryComponentChange()
    {
        var patch = new JsonPatchDocument<MiniLcm.Models.Entry>();
        var componentEntry = new Entry() { Id = Guid.NewGuid(), LexemeForm = { { "en", "component" } } };
        patch.Add(entry => entry.Components, ComplexFormComponent.FromEntries(_entry, componentEntry));
        var changes = Entry.ChangesFromJsonPatch(_entry, patch);
        var addEntryComponentChange =
            changes.Should().ContainSingle().Which.Should().BeOfType<AddEntryComponentChange>().Subject;
        addEntryComponentChange.ComplexFormEntryId.Should().Be(_entry.Id);
        addEntryComponentChange.ComponentEntryId.Should().Be(componentEntry.Id);
        addEntryComponentChange.ComponentHeadword.Should().Be(componentEntry.Headword());
    }

    [Fact]
    public void ChangesFromJsonPatch_RemoveComponentMakesDeleteChange()
    {
        var patch = new JsonPatchDocument<MiniLcm.Models.Entry>();
        var componentEntry = new Entry() { Id = Guid.NewGuid(), LexemeForm = { { "en", "component" } } };
        var complexFormComponent = ComplexFormComponent.FromEntries(_entry, componentEntry);
        _entry.Components.Add(complexFormComponent);
        patch.Remove(entry => entry.Components, 0);
        var changes = Entry.ChangesFromJsonPatch(_entry, patch);
        var removeEntryComponentChange = changes.Should().ContainSingle().Which.Should()
            .BeOfType<DeleteChange<CrdtComplexFormComponent>>().Subject;
        removeEntryComponentChange.EntityId.Should().Be(complexFormComponent.Id);
    }

    [Fact]
    public void ChangesFromJsonPatch_ReplaceComponentMakesReplaceEntryComponentChange()
    {
        var patch = new JsonPatchDocument<MiniLcm.Models.Entry>();
        var componentEntry = new Entry() { Id = Guid.NewGuid(), LexemeForm = { { "en", "component" } } };
        var complexFormComponent = ComplexFormComponent.FromEntries(_entry, componentEntry);
        _entry.Components.Add(complexFormComponent);
        var newComponentId = Guid.NewGuid();
        patch.Replace(entry => entry.Components, complexFormComponent with { ComponentEntryId = newComponentId, ComponentHeadword = "new" }, 0);
        var changes = Entry.ChangesFromJsonPatch(_entry, patch);
        var setComplexFormComponentChange = changes.Should().ContainSingle().Which.Should()
            .BeOfType<SetComplexFormComponentChange>().Subject;
        setComplexFormComponentChange.EntityId.Should().Be(complexFormComponent.Id);
        setComplexFormComponentChange.ComplexFormEntryId.Should().BeNull();
        setComplexFormComponentChange.ComponentEntryId.Should().Be(newComponentId);
        setComplexFormComponentChange.ComponentSenseId.Should().BeNull();
    }

    [Fact]
    public void ChangesFromJsonPatch_ReplaceComponentWithNewComplexFormIdMakesReplaceEntryComponentChange()
    {
        var patch = new JsonPatchDocument<MiniLcm.Models.Entry>();
        var componentEntry = new Entry() { Id = Guid.NewGuid(), LexemeForm = { { "en", "component" } } };
        var complexFormComponent = ComplexFormComponent.FromEntries(_entry, componentEntry);
        _entry.Components.Add(complexFormComponent);
        var newComplexFormId = Guid.NewGuid();
        patch.Replace(entry => entry.Components, complexFormComponent with { ComplexFormEntryId = newComplexFormId, ComplexFormHeadword = "new" }, 0);
        var changes = Entry.ChangesFromJsonPatch(_entry, patch);
        var setComplexFormComponentChange = changes.Should().ContainSingle().Which.Should()
            .BeOfType<SetComplexFormComponentChange>().Subject;
        setComplexFormComponentChange.EntityId.Should().Be(complexFormComponent.Id);
        setComplexFormComponentChange.ComponentEntryId.Should().BeNull();
        setComplexFormComponentChange.ComplexFormEntryId.Should().Be(newComplexFormId);
        setComplexFormComponentChange.ComponentSenseId.Should().BeNull();
    }

    [Fact]
    public void ChangesFromJsonPatch_AddComplexFormMakesAddEntryComponentChange()
    {
        var patch = new JsonPatchDocument<MiniLcm.Models.Entry>();
        var componentEntry = new Entry() { Id = Guid.NewGuid(), LexemeForm = { { "en", "complex form" } } };
        patch.Add(entry => entry.ComplexForms, ComplexFormComponent.FromEntries(_entry, componentEntry));
        var changes = Entry.ChangesFromJsonPatch(componentEntry, patch);
        var addEntryComponentChange =
            changes.Should().ContainSingle().Which.Should().BeOfType<AddEntryComponentChange>().Subject;
        addEntryComponentChange.ComplexFormEntryId.Should().Be(_entry.Id);
        addEntryComponentChange.ComponentEntryId.Should().Be(componentEntry.Id);
        addEntryComponentChange.ComponentHeadword.Should().Be(componentEntry.Headword());
    }

    [Fact]
    public void ChangesFromJsonPatch_RemoveComplexFormMakesDeleteChange()
    {
        var patch = new JsonPatchDocument<MiniLcm.Models.Entry>();
        var componentEntry = new Entry() { Id = Guid.NewGuid(), LexemeForm = { { "en", "component" } } };
        var complexFormComponent = ComplexFormComponent.FromEntries(_entry, componentEntry);
        componentEntry.ComplexForms.Add(complexFormComponent);
        patch.Remove(entry => entry.ComplexForms, 0);
        var changes = Entry.ChangesFromJsonPatch(componentEntry, patch);
        var removeEntryComponentChange = changes.Should().ContainSingle().Which.Should()
            .BeOfType<DeleteChange<CrdtComplexFormComponent>>().Subject;
        removeEntryComponentChange.EntityId.Should().Be(complexFormComponent.Id);
    }

    [Fact]
    public void ChangesFromJsonPatch_ReplaceComplexFormMakesReplaceEntryComponentChange()
    {
        var patch = new JsonPatchDocument<MiniLcm.Models.Entry>();
        var componentEntry = new Entry() { Id = Guid.NewGuid(), LexemeForm = { { "en", "component" } } };
        var complexFormComponent = ComplexFormComponent.FromEntries(_entry, componentEntry);
        componentEntry.ComplexForms.Add(complexFormComponent);
        var newComponentId = Guid.NewGuid();
        patch.Replace(entry => entry.ComplexForms, complexFormComponent with { ComponentEntryId = newComponentId, ComponentHeadword = "new" }, 0);
        var changes = Entry.ChangesFromJsonPatch(componentEntry, patch);
        var setComplexFormComponentChange = changes.Should().ContainSingle().Which.Should()
            .BeOfType<SetComplexFormComponentChange>().Subject;
        setComplexFormComponentChange.EntityId.Should().Be(complexFormComponent.Id);
        setComplexFormComponentChange.ComplexFormEntryId.Should().BeNull();
        setComplexFormComponentChange.ComponentEntryId.Should().Be(newComponentId);
        setComplexFormComponentChange.ComponentSenseId.Should().BeNull();
    }

    [Fact]
    public void ChangesFromJsonPatch_ReplaceComplexFormWithNewComplexFormIdMakesReplaceEntryComponentChange()
    {
        var patch = new JsonPatchDocument<MiniLcm.Models.Entry>();
        var componentEntry = new Entry() { Id = Guid.NewGuid(), LexemeForm = { { "en", "component" } } };
        var complexFormComponent = ComplexFormComponent.FromEntries(_entry, componentEntry);
        componentEntry.ComplexForms.Add(complexFormComponent);
        var newComplexFormId = Guid.NewGuid();
        patch.Replace(entry => entry.ComplexForms, complexFormComponent with { ComplexFormEntryId = newComplexFormId, ComplexFormHeadword = "new" }, 0);
        var changes = Entry.ChangesFromJsonPatch(componentEntry, patch);
        var setComplexFormComponentChange = changes.Should().ContainSingle().Which.Should()
            .BeOfType<SetComplexFormComponentChange>().Subject;
        setComplexFormComponentChange.EntityId.Should().Be(complexFormComponent.Id);
        setComplexFormComponentChange.ComponentEntryId.Should().BeNull();
        setComplexFormComponentChange.ComplexFormEntryId.Should().Be(newComplexFormId);
        setComplexFormComponentChange.ComponentSenseId.Should().BeNull();
    }
}
