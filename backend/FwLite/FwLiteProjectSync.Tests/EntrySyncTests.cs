using FwLiteProjectSync.Tests.Fixtures;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;

namespace FwLiteProjectSync.Tests;

public class EntrySyncTests : IClassFixture<SyncFixture>
{
    public EntrySyncTests(SyncFixture fixture)
    {
        _fixture = fixture;
    }

    private readonly SyncFixture _fixture;

    [Fact]
    public async Task CanChangeComplexFormVisSync_Components()
    {
        var component1 = await _fixture.CrdtApi.CreateEntry(new() { LexemeForm = { { "en", "component1" } } });
        var component2 = await _fixture.CrdtApi.CreateEntry(new() { LexemeForm = { { "en", "component2" } } });
        var complexFormId = Guid.NewGuid();
        var complexForm = await _fixture.CrdtApi.CreateEntry(new()
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
        Entry after = (Entry) complexForm.Copy();
        after.Components[0].ComponentEntryId = component2.Id;
        after.Components[0].ComponentHeadword = component2.Headword();

        await EntrySync.Sync(after, complexForm, _fixture.CrdtApi);

        var actual = await _fixture.CrdtApi.GetEntry(after.Id);
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(after);
    }

    [Fact]
    public async Task CanChangeComplexFormViaSync_ComplexForms()
    {
        var complexForm1 = await _fixture.CrdtApi.CreateEntry(new() { LexemeForm = { { "en", "complexForm1" } } });
        var complexForm2 = await _fixture.CrdtApi.CreateEntry(new() { LexemeForm = { { "en", "complexForm2" } } });
        var componentId = Guid.NewGuid();
        var component = await _fixture.CrdtApi.CreateEntry(new()
        {
            Id = componentId,
            LexemeForm = { { "en", "component" } },
            ComplexForms =
            [
                new ComplexFormComponent()
                {
                    ComponentEntryId = componentId,
                    ComponentHeadword = "component",
                    ComplexFormEntryId = complexForm1.Id,
                    ComplexFormHeadword = complexForm1.Headword()
                }
            ]
        });
        Entry after = (Entry) component.Copy();
        after.ComplexForms[0].ComplexFormEntryId = complexForm2.Id;
        after.ComplexForms[0].ComplexFormHeadword = complexForm2.Headword();

        await EntrySync.Sync(after, component, _fixture.CrdtApi);

        var actual = await _fixture.CrdtApi.GetEntry(after.Id);
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(after);
    }

    [Fact]
    public async Task CanChangeComplexFormTypeViaSync()
    {
        var entry = await _fixture.CrdtApi.CreateEntry(new() { LexemeForm = { { "en", "complexForm1" } } });
        var complexFormType = await _fixture.CrdtApi.GetComplexFormTypes().FirstAsync();
        var after = (Entry) entry.Copy();
        after.ComplexFormTypes = [complexFormType];
        await EntrySync.Sync(after, entry, _fixture.CrdtApi);

        var actual = await _fixture.CrdtApi.GetEntry(after.Id);
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(after);
    }
}
