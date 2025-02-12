using FwLiteProjectSync.Tests.Fixtures;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using MiniLcm.Tests.AutoFakerHelpers;
using Soenneker.Utils.AutoBogus;
using Soenneker.Utils.AutoBogus.Config;

namespace FwLiteProjectSync.Tests;

public class EntrySyncTests : IClassFixture<SyncFixture>
{
    private static readonly AutoFaker AutoFaker = new(new AutoFakerConfig()
    {
        RepeatCount = 5,
        Overrides =
        [
            new MultiStringOverride(),
            new ObjectWithIdOverride(),
            new OrderableOverride(),
        ]
    });

    public EntrySyncTests(SyncFixture fixture)
    {
        _fixture = fixture;
    }

    private readonly SyncFixture _fixture;

    [Fact]
    public async Task CanSyncRandomEntries()
    {
        var createdEntry = await _fixture.CrdtApi.CreateEntry(await AutoFaker.EntryReadyForCreation(_fixture.CrdtApi));
        var after = await AutoFaker.EntryReadyForCreation(_fixture.CrdtApi, entryId: createdEntry.Id);

        after.Senses = [.. AutoFaker.Faker.Random.Shuffle([
                // copy some senses over, so moves happen
                ..AutoFaker.Faker.Random.ListItems(createdEntry.Senses),
                ..after.Senses
            ])];

        await EntrySync.Sync(createdEntry, after, _fixture.CrdtApi);
        var actual = await _fixture.CrdtApi.GetEntry(after.Id);
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(after, options => options
            .For(e => e.Senses).Exclude(s => s.Order)
            .For(e => e.Senses).For(s => s.ExampleSentences).Exclude(s => s.Order)
            );
    }

    [Fact]
    public async Task CanChangeComplexFormViaSync_Components()
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

        await EntrySync.Sync(complexForm, after, _fixture.CrdtApi);

        var actual = await _fixture.CrdtApi.GetEntry(after.Id);
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(after, options => options);
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

        await EntrySync.Sync(component, after, _fixture.CrdtApi);

        var actual = await _fixture.CrdtApi.GetEntry(after.Id);
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(after, options => options);
    }

    [Fact]
    public async Task CanChangeComplexFormTypeViaSync()
    {
        var complexFormType = await _fixture.CrdtApi.CreateComplexFormType(new() { Name = new() { { "en", "complexFormType" } } });
        var entry = await _fixture.CrdtApi.CreateEntry(new() { LexemeForm = { { "en", "complexForm1" } } });
        var after = (Entry) entry.Copy();
        after.ComplexFormTypes = [complexFormType];
        await EntrySync.Sync(entry, after, _fixture.CrdtApi);

        var actual = await _fixture.CrdtApi.GetEntry(after.Id);
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(after, options => options);
    }
}
