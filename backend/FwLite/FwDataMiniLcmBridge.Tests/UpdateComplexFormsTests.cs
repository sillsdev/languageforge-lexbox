using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm;
using MiniLcm.Models;

namespace FwDataMiniLcmBridge.Tests;

[Collection(ProjectLoaderFixture.Name)]
public class UpdateComplexFormsTests(ProjectLoaderFixture fixture) : IAsyncLifetime
{
    private FwDataMiniLcmApi _api = null!;

    public Task InitializeAsync()
    {
        var projectName = "update-complex-forms-test_" + Guid.NewGuid();
        fixture.MockFwProjectLoader.NewProject(projectName, "en", "en");
        _api = fixture.CreateApi(projectName);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _api.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CanAddComponentToExistingEntry()
    {
        var complexForm = await _api.CreateEntry(new() { LexemeForm = { { "en", "complex form" } } });
        var component = await _api.CreateEntry(new() { LexemeForm = { { "en", "component" } } });

        await _api.UpdateEntry(complexForm.Id,
            new UpdateObjectInput<Entry>().Add(e => e.Components,
                ComplexFormComponent.FromEntries(complexForm, component)));
        var entry = await _api.GetEntry(complexForm.Id);
        entry.Should().NotBeNull();
        entry!.Components.Should().ContainSingle(c => c.ComponentEntryId == component.Id && c.ComplexFormEntryId == complexForm.Id);
    }

    [Fact]
    public async Task CanRemoveComponentFromExistingEntry()
    {
        var component = await _api.CreateEntry(new() { LexemeForm = { { "en", "component" } } });
        var complexFormId = Guid.NewGuid();
        var complexForm = await _api.CreateEntry(new()
        {
            Id = complexFormId,
            LexemeForm = { { "en", "complex form" } },
            Components = [new ComplexFormComponent()
            {
                ComponentEntryId = component.Id,
                ComponentHeadword = component.Headword(),
                ComplexFormEntryId = complexFormId,
                ComplexFormHeadword = "complex form"
            }]
        });

        await _api.UpdateEntry(complexForm.Id,
            new UpdateObjectInput<Entry>().Remove(e => e.Components, 0));
        var entry = await _api.GetEntry(complexForm.Id);
        entry.Should().NotBeNull();
        entry!.Components.Should().BeEmpty();
    }

    [Fact]
    public async Task CanChangeComponentId()
    {
        var component1 = await _api.CreateEntry(new() { LexemeForm = { { "en", "component1" } } });
        var component2 = await _api.CreateEntry(new() { LexemeForm = { { "en", "component2" } } });
        var complexFormId = Guid.NewGuid();
        var complexForm = await _api.CreateEntry(new()
        {
            Id = complexFormId,
            LexemeForm = { { "en", "complex form" } },
            Components =
            [
                new ComplexFormComponent()
                {
                    ComponentEntryId = component1.Id,
                    ComponentHeadword = component1.Headword(),
                    ComplexFormEntryId = complexFormId,
                    ComplexFormHeadword = "complex form"
                }
            ]
        });

        await _api.UpdateEntry(complexForm.Id, new UpdateObjectInput<Entry>().Set(e => e.Components[0].ComponentEntryId, component2.Id));
        var entry = await _api.GetEntry(complexForm.Id);
        entry.Should().NotBeNull();
        entry!.Components.Should().ContainSingle(c => c.ComponentEntryId == component2.Id);
    }

    [Fact]
    public async Task CanChangeComponentSenseId()
    {
        var component2SenseId = Guid.NewGuid();
        var component1 = await _api.CreateEntry(new() { LexemeForm = { { "en", "component1" } } });
        var component2 = await _api.CreateEntry(new() { LexemeForm = { { "en", "component2" } }, Senses = [new Sense() { Id = component2SenseId, Gloss = { { "en", "component2" } } }] });
        var complexFormId = Guid.NewGuid();
        var complexForm = await _api.CreateEntry(new()
        {
            Id = complexFormId,
            LexemeForm = { { "en", "complex form" } },
            Components =
            [
                new ComplexFormComponent()
                {
                    ComponentEntryId = component1.Id,
                    ComponentHeadword = component1.Headword(),
                    ComplexFormEntryId = complexFormId,
                    ComplexFormHeadword = "complex form"
                }
            ]
        });

        await _api.UpdateEntry(complexForm.Id, new UpdateObjectInput<Entry>().Set(e => e.Components[0].ComponentSenseId, component2SenseId));
        var entry = await _api.GetEntry(complexForm.Id);
        entry.Should().NotBeNull();
        entry!.Components.Should().ContainSingle(c => c.ComponentEntryId == component2.Id && c.ComponentSenseId == component2SenseId);
    }
    [Fact]
    public async Task CanChangeComponentSenseIdToNull()
    {
        var component2SenseId = Guid.NewGuid();
        var component2 = await _api.CreateEntry(new() { LexemeForm = { { "en", "component2" } }, Senses = [new Sense() { Id = component2SenseId, Gloss = { { "en", "component2" } } }] });
        var complexFormId = Guid.NewGuid();
        var complexForm = await _api.CreateEntry(new()
        {
            Id = complexFormId,
            LexemeForm = { { "en", "complex form" } },
            Components =
            [
                new ComplexFormComponent()
                {
                    ComponentEntryId = component2.Id,
                    ComponentSenseId = component2SenseId,
                    ComponentHeadword = component2.Headword(),
                    ComplexFormEntryId = complexFormId,
                    ComplexFormHeadword = "complex form"
                }
            ]
        });

        await _api.UpdateEntry(complexForm.Id, new UpdateObjectInput<Entry>().Set(e => e.Components[0].ComponentSenseId, null));
        var entry = await _api.GetEntry(complexForm.Id);
        entry.Should().NotBeNull();
        entry!.Components.Should().ContainSingle(c => c.ComponentEntryId == component2.Id && c.ComponentSenseId == null);
    }

    [Fact]
    public async Task CanChangeComplexFormId()
    {
        var component1 = await _api.CreateEntry(new() { LexemeForm = { { "en", "component1" } } });
        var complexForm2 = await _api.CreateEntry(new() { LexemeForm = { { "en", "complex form 2" } }});
        var complexFormId = Guid.NewGuid();
        var complexForm = await _api.CreateEntry(new()
        {
            Id = complexFormId,
            LexemeForm = { { "en", "complex form" } },
            Components =
            [
                new ComplexFormComponent()
                {
                    ComponentEntryId = component1.Id,
                    ComponentHeadword = component1.Headword(),
                    ComplexFormEntryId = complexFormId,
                    ComplexFormHeadword = "complex form"
                }
            ]
        });

        await _api.UpdateEntry(complexForm.Id, new UpdateObjectInput<Entry>().Set(e => e.Components[0].ComplexFormEntryId, complexForm2.Id));
        var entry = await _api.GetEntry(complexForm2.Id);
        entry.Should().NotBeNull();
        entry!.Components.Should().ContainSingle(c => c.ComponentEntryId == component1.Id);
    }

    [Fact]
    public async Task CanAddComplexFormToExistingEntry()
    {
        var complexForm = await _api.CreateEntry(new() { LexemeForm = { { "en", "complex form" } } });
        var component = await _api.CreateEntry(new() { LexemeForm = { { "en", "component" } } });

        await _api.UpdateEntry(component.Id,
            new UpdateObjectInput<Entry>().Add(e => e.ComplexForms,
                ComplexFormComponent.FromEntries(complexForm, component)));
        var entry = await _api.GetEntry(component.Id);
        entry.Should().NotBeNull();
        entry!.ComplexForms.Should().ContainSingle(c => c.ComponentEntryId == component.Id && c.ComplexFormEntryId == complexForm.Id);
    }

    [Fact]
    public async Task CanRemoveComplexFormFromExistingEntry()
    {
        var component = await _api.CreateEntry(new() { LexemeForm = { { "en", "component" } } });
        var complexFormId = Guid.NewGuid();
        await _api.CreateEntry(new()
        {
            Id = complexFormId,
            LexemeForm = { { "en", "complex form" } },
            Components = [new ComplexFormComponent()
            {
                ComponentEntryId = component.Id,
                ComponentHeadword = component.Headword(),
                ComplexFormEntryId = complexFormId,
                ComplexFormHeadword = "complex form"
            }]
        });

        await _api.UpdateEntry(component.Id,
            new UpdateObjectInput<Entry>().Remove(e => e.ComplexForms, 0));
        var entry = await _api.GetEntry(component.Id);
        entry.Should().NotBeNull();
        entry!.ComplexForms.Should().BeEmpty();
    }

    [Fact]
    public async Task CanChangeComplexFormIdOnComplexFormsList()
    {
        var component1 = await _api.CreateEntry(new() { LexemeForm = { { "en", "component1" } } });
        var complexForm2 = await _api.CreateEntry(new() { LexemeForm = { { "en", "complex form 2" } } });
        var complexFormId = Guid.NewGuid();
        await _api.CreateEntry(new()
        {
            Id = complexFormId,
            LexemeForm = { { "en", "complex form" } },
            Components =
            [
                new ComplexFormComponent()
                {
                    ComponentEntryId = component1.Id,
                    ComponentHeadword = component1.Headword(),
                    ComplexFormEntryId = complexFormId,
                    ComplexFormHeadword = "complex form"
                }
            ]
        });

        await _api.UpdateEntry(component1.Id, new UpdateObjectInput<Entry>().Set(e => e.ComplexForms[0].ComplexFormEntryId, complexForm2.Id));
        var entry = await _api.GetEntry(component1.Id);
        entry.Should().NotBeNull();
        entry!.ComplexForms.Should().ContainSingle(c => c.ComplexFormEntryId == complexForm2.Id);
    }

    [Fact]
    public async Task CanChangeComponentIdOnComplexFormsList()
    {
        var component1 = await _api.CreateEntry(new() { LexemeForm = { { "en", "component1" } } });
        var component2 = await _api.CreateEntry(new() { LexemeForm = { { "en", "component2" } } });
        var complexFormId = Guid.NewGuid();
        await _api.CreateEntry(new()
        {
            Id = complexFormId,
            LexemeForm = { { "en", "complex form" } },
            Components =
            [
                new ComplexFormComponent()
                {
                    ComponentEntryId = component1.Id,
                    ComponentHeadword = component1.Headword(),
                    ComplexFormEntryId = complexFormId,
                    ComplexFormHeadword = "complex form"
                }
            ]
        });

        await _api.UpdateEntry(component1.Id, new UpdateObjectInput<Entry>().Set(e => e.ComplexForms[0].ComponentEntryId, component2.Id));
        var entry = await _api.GetEntry(component2.Id);
        entry.Should().NotBeNull();
        entry!.ComplexForms.Should().ContainSingle(c => c.ComponentEntryId == component2.Id && c.ComplexFormEntryId == complexFormId);
    }
}
