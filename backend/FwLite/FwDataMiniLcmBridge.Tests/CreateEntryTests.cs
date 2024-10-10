using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm.Models;

namespace FwDataMiniLcmBridge.Tests;

[Collection(ProjectLoaderFixture.Name)]
public class CreateEntryTests(ProjectLoaderFixture fixture) : IAsyncLifetime
{
    private FwDataMiniLcmApi _api = null!;

    public Task InitializeAsync()
    {
        var projectName = "create-entry-test_" + Guid.NewGuid();
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
    public async Task CanCreateEntry()
    {
        var entry = await _api.CreateEntry(new() { LexemeForm = { { "en", "test" } } });
        entry.Should().NotBeNull();
        entry!.LexemeForm.Values.Should().ContainKey("en");
        entry.LexemeForm.Values["en"].Should().Be("test");
    }

    [Fact]
    public async Task CanCreate_WithComponentsProperty()
    {
        var component = await _api.CreateEntry(new() { LexemeForm = { { "en", "test component" } } });
        var entryId = Guid.NewGuid();
        var entry = await _api.CreateEntry(new()
        {
            Id = entryId,
            LexemeForm = { { "en", "test" } },
            Components =
            [
                new ComplexFormComponent()
                {
                    ComponentEntryId = component.Id,
                    ComponentHeadword = component.Headword(),
                    ComplexFormEntryId = entryId,
                    ComplexFormHeadword = "test"
                }
            ]
        });
        entry = await _api.GetEntry(entry.Id);
        entry.Should().NotBeNull();
        entry!.Components.Should().ContainSingle(c => c.ComponentEntryId == component.Id);
    }

    [Fact]
    public async Task CanCreate_WithComplexFormsProperty()
    {
        var complexForm = await _api.CreateEntry(new() { LexemeForm = { { "en", "test complex form" } } });
        var entryId = Guid.NewGuid();
        var entry = await _api.CreateEntry(new()
        {
            Id = entryId,
            LexemeForm = { { "en", "test" } },
            ComplexForms =
            [
                new ComplexFormComponent()
                {
                    ComponentEntryId = entryId,
                    ComponentHeadword = "test",
                    ComplexFormEntryId = complexForm.Id,
                    ComplexFormHeadword = "test complex form"
                }
            ]
        });
        entry = await _api.GetEntry(entry.Id);
        entry.Should().NotBeNull();
        entry!.ComplexForms.Should().ContainSingle(c => c.ComplexFormEntryId == complexForm.Id);
    }

    [Fact]
    public async Task CreateEntry_WithComponentSenseDoesNotShowOnComplexFormsList()
    {
        var componentSenseId = Guid.NewGuid();
        var component = await _api.CreateEntry(new()
        {
            LexemeForm = { { "en", "test component" } },
            Senses = [new Sense() { Id = componentSenseId, Gloss = { { "en", "test component sense" } } }]
        });
        var complexFormEntryId = Guid.NewGuid();
        await _api.CreateEntry(new()
        {
            Id = complexFormEntryId,
            LexemeForm = { { "en", "test" } },
            Components =
            [
                new ComplexFormComponent()
                {
                    ComponentEntryId = component.Id,
                    ComponentHeadword = component.Headword(),
                    ComponentSenseId = componentSenseId,
                    ComplexFormEntryId = complexFormEntryId,
                    ComplexFormHeadword = "test"
                }
            ]
        });

        var entry = await _api.GetEntry(component.Id);
        entry.Should().NotBeNull();
        entry!.ComplexForms.Should().BeEmpty();

        entry = await _api.GetEntry(complexFormEntryId);
        entry.Should().NotBeNull();
        entry!.Components.Should().ContainSingle(c => c.ComplexFormEntryId == complexFormEntryId && c.ComponentEntryId == component.Id && c.ComponentSenseId == componentSenseId);
    }

    [Fact]
    public async Task CanCreate_WithComplexFormTypesProperty()
    {
        var complexFormType = await _api.CreateComplexFormType(new()
        {
            Name = new MultiString() { { "en", "test complex form type" } }
        });

        var entry = await _api.CreateEntry(new()
        {
            LexemeForm = { { "en", "test" } },
            ComplexFormTypes = [complexFormType]
        });
        entry = await _api.GetEntry(entry.Id);
        entry.Should().NotBeNull();
        entry!.ComplexFormTypes.Should().ContainSingle(c => c.Id == complexFormType.Id);
    }
}
