using FwLiteProjectSync.Tests.Fixtures;
using MiniLcm;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using MiniLcm.Tests.AutoFakerHelpers;
using Soenneker.Utils.AutoBogus;
using Soenneker.Utils.AutoBogus.Config;

namespace FwLiteProjectSync.Tests;

public class EntrySyncTests : IClassFixture<SyncFixture>, IAsyncLifetime
{
    private static readonly AutoFaker AutoFaker = new(new AutoFakerConfig()
    {
        RepeatCount = 5,
        Overrides =
        [
            new MultiStringOverride(),
            new RichMultiStringOverride(),
            new ObjectWithIdOverride(),
            new OrderableOverride(),
        ]
    });

    public EntrySyncTests(SyncFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.EnsureDefaultVernacularWritingSystemExistsInCrdt();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
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
            .For(e => e.Components).Exclude(c => c.Order)
            .For(e => e.ComplexForms).Exclude(c => c.Order)
            .For(e => e.Senses).For(s => s.ExampleSentences).Exclude(e => e.Order)
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

    [Theory]
    [InlineData(true, ProjectDataFormat.Harmony)]
    [InlineData(true, ProjectDataFormat.FwData)]
    [InlineData(false, ProjectDataFormat.Harmony)]
    [InlineData(false, ProjectDataFormat.FwData)]
    public async Task CanInsertComplexFormComponentViaSync(bool componentThenComplexForm, ProjectDataFormat apiType)
    {
        // arrange
        IMiniLcmApi api = apiType == ProjectDataFormat.Harmony ? _fixture.CrdtApi : _fixture.FwDataApi;

        var existingComponent = await api.CreateEntry(new() { LexemeForm = { { "en", "existing-component" } } });
        var complexFormBefore = await api.CreateEntry(new() { LexemeForm = { { "en", "complex-form" } } });
        await api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(complexFormBefore, existingComponent));
        complexFormBefore = (await api.GetEntry(complexFormBefore.Id))!;

        var newComponentBefore = await api.CreateEntry(new() { LexemeForm = { { "en", "component" } } });
        var newComplexFormComponent = ComplexFormComponent.FromEntries(complexFormBefore, newComponentBefore);

        var newComponentAfter = newComponentBefore.Copy();
        newComponentAfter.ComplexForms.Add(newComplexFormComponent);
        var complexFormAfter = complexFormBefore.Copy();
        complexFormAfter.Components.Insert(0, newComplexFormComponent);

        // act - The big question is: both entries want to add the same component to the complex form. One cares about its order, the other doesn't. Will it land in the right place?
        if (componentThenComplexForm)
        {
            // this results in 2 crdt changes:
            // (1) add complex-form (i.e. implicitly add component)
            // (2) move component to the right place
            await EntrySync.Sync([newComponentBefore, complexFormBefore], [newComponentAfter, complexFormAfter], api);
        }
        else
        {
            // this results in 1 crdt change:
            // the component is added in the right place
            // (adding the complex-form becomes a no-op, because it already exists and a BetweenPosition is not specified)
            await EntrySync.Sync([complexFormBefore, newComponentBefore], [complexFormAfter, newComponentAfter], api);
        }

        // assert
        var actual = await api.GetEntry(complexFormAfter.Id);
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(complexFormAfter, options => options
            .WithStrictOrdering()
            .For(e => e.Components).Exclude(c => c.Order)
            .For(e => e.Components).Exclude(c => c.Id));
        actual.Components.Count.Should().Be(2);
        actual.Components.First().Should().BeEquivalentTo(newComplexFormComponent, options => options
            .Excluding(c => c.Order)
            .Excluding(c => c.Id));
    }
}
