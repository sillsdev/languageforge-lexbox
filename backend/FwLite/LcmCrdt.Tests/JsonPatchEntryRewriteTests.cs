using LcmCrdt.Changes.Entries;
using LcmCrdt.Objects;
using SIL.Harmony.Changes;
using SystemTextJsonPatch;

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
        var changes = _entry.ToChanges(patch);
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
        var changes = _entry.ToChanges(patch);
        var removeEntryComponentChange = changes.Should().ContainSingle().Which.Should()
            .BeOfType<DeleteChange<ComplexFormComponent>>().Subject;
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
        var changes = _entry.ToChanges(patch);
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
        var changes = _entry.ToChanges(patch);
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
        var changes = componentEntry.ToChanges(patch);
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
        var changes = componentEntry.ToChanges(patch);
        var removeEntryComponentChange = changes.Should().ContainSingle().Which.Should()
            .BeOfType<DeleteChange<ComplexFormComponent>>().Subject;
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
        var changes = componentEntry.ToChanges(patch);
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
        var changes = componentEntry.ToChanges(patch);
        var setComplexFormComponentChange = changes.Should().ContainSingle().Which.Should()
            .BeOfType<SetComplexFormComponentChange>().Subject;
        setComplexFormComponentChange.EntityId.Should().Be(complexFormComponent.Id);
        setComplexFormComponentChange.ComponentEntryId.Should().BeNull();
        setComplexFormComponentChange.ComplexFormEntryId.Should().Be(newComplexFormId);
        setComplexFormComponentChange.ComponentSenseId.Should().BeNull();
    }

    [Fact]
    public void ChangesFromJsonPatch_AddComplexFormTypeMakesAddComplexFormTypeChange()
    {
        var patch = new JsonPatchDocument<MiniLcm.Models.Entry>();
        var complexFormType = new ComplexFormType() { Id = Guid.NewGuid(), Name = new MultiString() };
        patch.Add(entry => entry.ComplexFormTypes, complexFormType);
        var changes = _entry.ToChanges(patch);
        var addComplexFormTypeChange =
            changes.Should().ContainSingle().Which.Should().BeOfType<AddComplexFormTypeChange>().Subject;
        addComplexFormTypeChange.EntityId.Should().Be(_entry.Id);
        addComplexFormTypeChange.ComplexFormType.Should().Be(complexFormType);
    }

    [Fact]
    public void ChangesFromJsonPatch_RemoveComplexFormTypeMakesRemoveComplexFormTypeChange()
    {
        var patch = new JsonPatchDocument<MiniLcm.Models.Entry>();
        var complexFormType = new ComplexFormType() { Id = Guid.NewGuid(), Name = new MultiString() };
        _entry.ComplexFormTypes.Add(complexFormType);
        patch.Remove(entry => entry.ComplexFormTypes, 0);
        var changes = _entry.ToChanges(patch);
        var removeComplexFormTypeChange = changes.Should().ContainSingle().Which.Should()
            .BeOfType<RemoveComplexFormTypeChange>().Subject;
        removeComplexFormTypeChange.EntityId.Should().Be(_entry.Id);
        removeComplexFormTypeChange.ComplexFormTypeId.Should().Be(complexFormType.Id);
    }

    [Fact]
    public void ChangesFromJsonPatch_ReplaceComplexFormTypeMakesReplaceComplexFormTypeChange()
    {
        var patch = new JsonPatchDocument<MiniLcm.Models.Entry>();
        var complexFormType = new ComplexFormType() { Id = Guid.NewGuid(), Name = new MultiString() };
        _entry.ComplexFormTypes.Add(complexFormType);
        var newComplexFormType = new ComplexFormType() { Id = Guid.NewGuid(), Name = new MultiString() };
        patch.Replace(entry => entry.ComplexFormTypes, newComplexFormType, 0);
        var changes = _entry.ToChanges(patch);
        var replaceComplexFormTypeChange = changes.Should().ContainSingle().Which.Should()
        .BeOfType<ReplaceComplexFormTypeChange>().Subject;
        replaceComplexFormTypeChange.EntityId.Should().Be(_entry.Id);
        replaceComplexFormTypeChange.OldComplexFormTypeId.Should().Be(complexFormType.Id);
        replaceComplexFormTypeChange.NewComplexFormType.Should().Be(newComplexFormType);
    }
}
