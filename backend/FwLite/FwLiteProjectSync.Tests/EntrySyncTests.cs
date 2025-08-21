using FwLiteProjectSync.Tests.Fixtures;
using MiniLcm;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using MiniLcm.Tests.AutoFakerHelpers;
using Soenneker.Utils.AutoBogus;

namespace FwLiteProjectSync.Tests;

public class CrdtEntrySyncTests(SyncFixture fixture) : EntrySyncTestsBase(fixture)
{
    private static readonly AutoFaker AutoFaker = new(AutoFakerDefault.Config);

    protected override IMiniLcmApi GetApi(SyncFixture fixture)
    {
        return fixture.CrdtApi;
    }

    [Fact]
    public async Task CanSyncRandomEntries()
    {
        var createdEntry = await Api.CreateEntry(await AutoFaker.EntryReadyForCreation(Api));
        var after = await AutoFaker.EntryReadyForCreation(Api, entryId: createdEntry.Id);

        after.Senses = [.. AutoFaker.Faker.Random.Shuffle([
                // copy some senses over, so moves happen
                ..AutoFaker.Faker.Random.ListItems(createdEntry.Senses),
                ..after.Senses
            ])];

        await EntrySync.SyncFull(createdEntry, after, Api);
        var actual = await Api.GetEntry(after.Id);
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(after, options => options
            .For(e => e.Senses).Exclude(s => s.Order)
            .For(e => e.Components).Exclude(c => c.Order)
            .For(e => e.ComplexForms).Exclude(c => c.Order)
            .For(e => e.Senses).For(s => s.ExampleSentences).Exclude(e => e.Order)
            );
    }
}

public class FwDataEntrySyncTests(SyncFixture fixture) : EntrySyncTestsBase(fixture)
{
    protected override IMiniLcmApi GetApi(SyncFixture fixture)
    {
        return fixture.FwDataApi;
    }
}

public abstract class EntrySyncTestsBase(SyncFixture fixture) : IClassFixture<SyncFixture>, IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await _fixture.EnsureDefaultVernacularWritingSystemExistsInCrdt();
        Api = GetApi(_fixture);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    protected abstract IMiniLcmApi GetApi(SyncFixture fixture);

    private readonly SyncFixture _fixture = fixture;
    protected IMiniLcmApi Api = null!;

    [Fact]
    public async Task CanChangeComplexFormViaSync_Components()
    {
        var component1 = await Api.CreateEntry(new() { LexemeForm = { { "en", "component1" } } });
        var component2 = await Api.CreateEntry(new() { LexemeForm = { { "en", "component2" } } });
        var complexFormId = Guid.NewGuid();
        var complexForm = await Api.CreateEntry(new()
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
        var complexFormAfter = complexForm.Copy();
        complexFormAfter.Components[0].ComponentEntryId = component2.Id;
        complexFormAfter.Components[0].ComponentHeadword = component2.Headword();

        await EntrySync.SyncFull(complexForm, complexFormAfter, Api);

        var actual = await Api.GetEntry(complexFormAfter.Id);
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(complexFormAfter, options => options
            .For(e => e.Components).Exclude(c => c.Id));
    }

    [Fact]
    public async Task CanChangeComplexFormViaSync_ComplexForms()
    {
        var complexForm1 = await Api.CreateEntry(new() { LexemeForm = { { "en", "complexForm1" } } });
        var complexForm2 = await Api.CreateEntry(new() { LexemeForm = { { "en", "complexForm2" } } });
        var componentId = Guid.NewGuid();
        var component = await Api.CreateEntry(new()
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
        var componentAter = component.Copy();
        componentAter.ComplexForms[0].ComplexFormEntryId = complexForm2.Id;
        componentAter.ComplexForms[0].ComplexFormHeadword = complexForm2.Headword();

        await EntrySync.SyncFull(component, componentAter, Api);

        var actual = await Api.GetEntry(componentAter.Id);
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(componentAter, options => options
            .For(e => e.ComplexForms).Exclude(c => c.Id));
    }

    [Fact]
    public async Task CanChangeComplexFormTypeViaSync()
    {
        var complexFormType = await Api.CreateComplexFormType(new() { Name = new() { { "en", "complexFormType" } } });
        var entry = await Api.CreateEntry(new() { LexemeForm = { { "en", "complexForm1" } } });
        var after = entry.Copy();
        after.ComplexFormTypes = [complexFormType];
        await EntrySync.SyncFull(entry, after, Api);

        var actual = await Api.GetEntry(after.Id);
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(after, options => options);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanInsertComplexFormComponentViaSync(bool componentThenComplexForm)
    {
        // arrange
        var existingComponent = await Api.CreateEntry(new() { LexemeForm = { { "en", "existing-component" } } });
        var complexFormBefore = await Api.CreateEntry(new() { LexemeForm = { { "en", "complex-form" } } });
        await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(complexFormBefore, existingComponent));
        complexFormBefore = (await Api.GetEntry(complexFormBefore.Id))!;

        var newComponentBefore = await Api.CreateEntry(new() { LexemeForm = { { "en", "component" } } });
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
            await EntrySync.SyncFull([newComponentBefore, complexFormBefore], [newComponentAfter, complexFormAfter], Api);
        }
        else
        {
            // this results in 1 crdt change:
            // the component is added in the right place
            // (adding the complex-form becomes a no-op, because it already exists and a BetweenPosition is not specified)
            await EntrySync.SyncFull([complexFormBefore, newComponentBefore], [complexFormAfter, newComponentAfter], Api);
        }

        // assert
        var actual = await Api.GetEntry(complexFormAfter.Id);
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

    [Fact]
    public async Task CanSyncNewEntryReferencedByExistingEntry()
    {
        // arrange
        // - before
        var existingEntryBefore = await Api.CreateEntry(new() { LexemeForm = { { "en", "existing-component" } } });

        // - after
        var existingEntryAfter = existingEntryBefore.Copy();
        var newEntry = new Entry() { Id = Guid.NewGuid(), LexemeForm = { { "en", "complex-form" } } };
        var newComplexFormComponent = ComplexFormComponent.FromEntries(newEntry, existingEntryAfter);
        existingEntryAfter.ComplexForms.Add(newComplexFormComponent);
        newEntry.Components.Add(newComplexFormComponent);

        // act
        await EntrySync.SyncFull([existingEntryBefore], [existingEntryAfter, newEntry], Api);

        // assert
        var actualExistingEntry = await Api.GetEntry(existingEntryAfter.Id);
        actualExistingEntry.Should().BeEquivalentTo(existingEntryAfter, options => options
            .For(e => e.ComplexForms).Exclude(c => c.Id)
            .For(e => e.ComplexForms).Exclude(c => c.Order));

        var actualNewEntry = await Api.GetEntry(newEntry.Id);
        actualNewEntry.Should().BeEquivalentTo(newEntry, options => options
            .Excluding(e => e.ComplexFormTypes) // LibLcm automatically creates a complex form type. Should we?
            .For(e => e.Components).Exclude(c => c.Id)
            .For(e => e.Components).Exclude(c => c.Order));
    }

    [Fact]
    public async Task CanSyncNewComplexFormComponentReferencingNewSense()
    {
        // arrange
        // - before
        var complexFormEntryBefore = await Api.CreateEntry(new() { LexemeForm = { { "en", "complex-form" } } });
        var componentEntryBefore = await Api.CreateEntry(new() { LexemeForm = { { "en", "component" } } });

        // - after
        var complexFormEntryAfter = complexFormEntryBefore.Copy();
        var componentEntryAfter = componentEntryBefore.Copy();
        var senseId = Guid.NewGuid();
        componentEntryAfter.Senses = [new Sense() { Id = senseId, EntryId = componentEntryAfter.Id }];

        var component = ComplexFormComponent.FromEntries(complexFormEntryAfter, componentEntryAfter, senseId);
        complexFormEntryAfter.Components.Add(component);
        componentEntryAfter.ComplexForms.Add(component);

        // act
        await EntrySync.SyncFull(
            // note: the entry with the added sense is at the end of the list
            [complexFormEntryBefore, componentEntryBefore],
            [complexFormEntryAfter, componentEntryAfter],
            Api);

        // assert
        var actualComplexFormEntry = await Api.GetEntry(complexFormEntryAfter.Id);
        actualComplexFormEntry.Should().BeEquivalentTo(complexFormEntryAfter,
            options => SyncTests.SyncExclusions(options)
            .Excluding(e => e.ComplexFormTypes) // LibLcm automatically creates a complex form type. Should we?
            .WithStrictOrdering());

        var actualComponentEntry = await Api.GetEntry(componentEntryAfter.Id);
        actualComponentEntry.Should().BeEquivalentTo(componentEntryAfter,
            options => SyncTests.SyncExclusions(options).WithStrictOrdering());
    }

    [Fact]
    public async Task SyncsAddedEntriesInTwoPhases()
    {
        // Arrange
        // - after
        var component = new Entry()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "component" } }
        };
        var complexForm = new Entry()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "complex form" } }
        };
        var complexFormComponent = ComplexFormComponent.FromEntries(complexForm, component);
        component.ComplexForms.Add(complexFormComponent);
        complexForm.Components.Add(complexFormComponent);

        // act - first phase
        await EntrySync.SyncWithoutComplexFormsAndComponents([], [component, complexForm], Api);

        // assert - first phase
        var actualComponent = await Api.GetEntry(component.Id);
        actualComponent.Should().BeEquivalentTo(component,
            options => options.Excluding(e => e.ComplexForms));
        actualComponent.ComplexForms.Should().BeEmpty();

        var actualComplexForm = await Api.GetEntry(complexForm.Id);
        actualComplexForm.Should().BeEquivalentTo(complexForm,
            options => options.Excluding(e => e.Components));
        actualComplexForm.Components.Should().BeEmpty();

        // act - second phase
        await EntrySync.SyncComplexFormsAndComponentsOfExistingEntries([], [component, complexForm], Api);

        // assert - second phase
        actualComponent = await Api.GetEntry(component.Id);
        actualComponent.Should().BeEquivalentTo(component,
            options => SyncTests.SyncExclusions(options).WithStrictOrdering());

        actualComplexForm = await Api.GetEntry(complexForm.Id);
        actualComplexForm.Should().BeEquivalentTo(complexForm,
            options => SyncTests.SyncExclusions(options)
            .Excluding(e => e.ComplexFormTypes) // LibLcm automatically creates a complex form type. Should we?
            .WithStrictOrdering());
    }

    [Fact]
    public async Task SyncsUpdatedEntriesInTwoPhases()
    {
        // Arrange
        // - before
        var componentBefore = await Api.CreateEntry(new() { LexemeForm = { { "en", "component" } } });

        // - after
        var componentAfter = componentBefore.Copy();
        componentAfter.LexemeForm["en"] = "component updated";
        var complexForm = new Entry()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "complex form" } }
        };
        var complexFormComponent = ComplexFormComponent.FromEntries(complexForm, componentAfter);
        componentAfter.ComplexForms.Add(complexFormComponent);
        complexForm.Components.Add(complexFormComponent);

        // act - first phase
        await EntrySync.SyncWithoutComplexFormsAndComponents([componentBefore], [componentAfter, complexForm], Api);

        // assert - first phase
        var actualComponent = await Api.GetEntry(componentAfter.Id);
        actualComponent.Should().BeEquivalentTo(componentAfter,
            options => options.Excluding(e => e.ComplexForms));
        actualComponent.ComplexForms.Should().BeEmpty();

        var actualComplexForm = await Api.GetEntry(complexForm.Id);
        actualComplexForm.Should().BeEquivalentTo(complexForm,
            options => options.Excluding(e => e.Components));
        actualComplexForm.Components.Should().BeEmpty();

        // act - second phase
        await EntrySync.SyncComplexFormsAndComponentsOfExistingEntries([], [componentAfter, complexForm], Api);

        // assert - second phase
        actualComponent = await Api.GetEntry(componentAfter.Id);
        actualComponent.Should().BeEquivalentTo(componentAfter,
            options => SyncTests.SyncExclusions(options).WithStrictOrdering());

        actualComplexForm = await Api.GetEntry(complexForm.Id);
        actualComplexForm.Should().BeEquivalentTo(complexForm,
            options => SyncTests.SyncExclusions(options)
            .Excluding(e => e.ComplexFormTypes) // LibLcm automatically creates a complex form type. Should we?
            .WithStrictOrdering());
    }
}
