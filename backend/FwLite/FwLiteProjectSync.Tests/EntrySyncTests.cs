using System.Text;
using FwDataMiniLcmBridge.Api;
using FwLiteProjectSync.Tests.Fixtures;
using LcmCrdt;
using MiniLcm;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using MiniLcm.Tests.AutoFakerHelpers;
using Soenneker.Utils.AutoBogus;

namespace FwLiteProjectSync.Tests;

public class CrdtEntrySyncTests(ExtraWritingSystemsSyncFixture fixture) : EntrySyncTestsBase(fixture)
{
    protected override IMiniLcmApi GetApi(SyncFixture fixture)
    {
        return fixture.CrdtApi;
    }
}

public class FwDataEntrySyncTests(ExtraWritingSystemsSyncFixture fixture) : EntrySyncTestsBase(fixture)
{
    protected override IMiniLcmApi GetApi(SyncFixture fixture)
    {
        return fixture.FwDataApi;
    }

    // this will notify us when we start syncing MorphType (if that ever happens)
    [Fact]
    public async Task FwDataApiDoesNotUpdateMorphType()
    {
        // arrange
        var entry = await Api.CreateEntry(new()
        {
            LexemeForm = { { "en", "morph-type-test" } },
            MorphType = MorphType.BoundStem
        });

        // act
        var updatedEntry = entry.Copy();
        updatedEntry.MorphType = MorphType.Suffix;
        await EntrySync.SyncFull(entry, updatedEntry, Api);

        // assert
        var actual = await Api.GetEntry(entry.Id);
        actual.Should().NotBeNull();
        actual.MorphType.Should().Be(MorphType.BoundStem);
    }
}

public abstract class EntrySyncTestsBase(ExtraWritingSystemsSyncFixture fixture) : IClassFixture<ExtraWritingSystemsSyncFixture>, IAsyncLifetime
{
    public Task InitializeAsync()
    {
        Api = GetApi(_fixture);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    protected abstract IMiniLcmApi GetApi(SyncFixture fixture);

    private readonly SyncFixture _fixture = fixture;
    protected IMiniLcmApi Api = null!;

    private static readonly AutoFaker AutoFaker = new(AutoFakerDefault.MakeConfig(
        ExtraWritingSystemsSyncFixture.VernacularWritingSystems));

    public enum ApiType
    {
        Crdt,
        FwData
    }

    // The round-tripping api is not what is under test here. It's purely for preprocessing.
    // It's so that that the data under test is being read from a real API (i.e. fwdata or crdt)
    // and thus reflects whatever nuances that API may have.
    //
    // Not all of the test cases are realistic, but they should all work and they reflect the idea
    // that "any MiniLcmApi implementation should be compatible with any other implementation".
    // Even the unrealistic test cases could potentially expose unexpected, undesirable nuances in API behaviour.
    // They also reflect the diversity of pipelines real entries might go through.
    // For example, a currently real scenario is that "after" is read from fwdata and "before" is read from crdt
    // and then round-tripped through a json file.
    // That case is not explicitly covered here.
    //
    // The most critical test cases are:
    // Api == CrdtApi and RoundTripApi == FwDataApi
    // Api == FwDataApi and RoundTripApi == CrdtApi
    // (though, as noted above, this case doesn't perfectly reflect real usage)
    [Theory]
    [InlineData(ApiType.Crdt)]
    [InlineData(ApiType.FwData)]
    [InlineData(null)]
    public async Task CanSyncRandomEntries(ApiType? roundTripApiType)
    {
        // arrange
        var currentApiType = Api switch
        {
            FwDataMiniLcmApi => ApiType.FwData,
            CrdtMiniLcmApi => ApiType.Crdt,
            // This works now, because we're not currently wrapping Api,
            // but if we ever do, then we want this to throw, so we know we need to detect the api differently.
            _ => throw new InvalidOperationException("Unknown API type")
        };

        IMiniLcmApi? roundTripApi = roundTripApiType switch
        {
            ApiType.Crdt => _fixture.CrdtApi,
            ApiType.FwData => _fixture.FwDataApi,
            _ => null
        };

        var before = AutoFaker.Generate<Entry>();
        var after = AutoFaker.Generate<Entry>();
        after.Id = before.Id;

        // We have to "prepare" while before and after have no overlap (i.e. before we start mixing parts of before into after),
        // otherwise "PrepareToCreateEntry" would fail due to trying to create duplicate related entities.
        // After this we can't ADD anything to after that has dependencies
        // e.g. ExampleSentences are fine, because they're owned/part of an entry.
        // Parts of speech, on the other hand, are not owned by an entry.
        await Api.PrepareToCreateEntry(before);
        await Api.PrepareToCreateEntry(after);

        if (roundTripApi is not null && currentApiType != roundTripApiType)
        {
            await roundTripApi.PrepareToCreateEntry(before);
            await roundTripApi.PrepareToCreateEntry(after);
        }

        // keep some old senses, remove others
        var someRandomBeforeSenses = AutoFaker.Faker.Random.ListItems(before.Senses).Select(createdSense =>
        {
            var copy = createdSense.Copy();
            copy.ExampleSentences = [
                // shuffle to cause moves
                ..AutoFaker.Faker.Random.Shuffle([
                    // keep some, remove others
                    ..AutoFaker.Faker.Random.ListItems(copy.ExampleSentences),
                    // add new
                    AutoFaker.ExampleSentence(copy),
                    AutoFaker.ExampleSentence(copy),
                ]),
            ];
            return copy;
        });
        // keep new, and shuffle to cause moves
        after.Senses = [.. AutoFaker.Faker.Random.Shuffle([.. someRandomBeforeSenses, .. after.Senses])];

        after.ComplexForms = [
            // shuffle to cause moves
            ..AutoFaker.Faker.Random.Shuffle([
                // keep some, remove others
                ..AutoFaker.Faker.Random.ListItems(before.ComplexForms)
                .Select(createdCfc =>
                {
                    var copy = createdCfc.Copy();
                    copy.ComponentHeadword = after.Headword();
                    return copy;
                }),
                // keep new
                ..after.ComplexForms
            ]),
        ];

        after.Components = [
            // shuffle to cause moves
            ..AutoFaker.Faker.Random.Shuffle([
                // keep some, remove others
                ..AutoFaker.Faker.Random.ListItems(before.Components)
                .Select(createdCfc =>
                {
                    var copy = createdCfc.Copy();
                    copy.ComplexFormHeadword = after.Headword();
                    return copy;
                }),
                // keep new
                ..after.Components
            ]),
        ];

        // expected should not be round-tripped, because an api might manipulate it somehow.
        // We expect the final result to be equivalent to this "raw"/untouched, requested state.
        var expected = after.Copy();

        if (roundTripApi is not null)
        {
            // round-tripping ensures we're dealing with realistic data
            // (e.g. in fwdata ComplexFormComponents do not have an Id)
            before = await roundTripApi.CreateEntry(before);
            await roundTripApi.DeleteEntry(before.Id);
            after = await roundTripApi.CreateEntry(after);
            await roundTripApi.DeleteEntry(after.Id);
        }

        // before should not be round-tripped here. That's handled above.
        await Api.CreateEntry(before);

        // act
        await EntrySync.SyncFull(before, after, Api);
        var actual = await Api.GetEntry(after.Id);

        // assert
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(after, options =>
        {
            options = options
                .WithStrictOrdering()
                .WithoutStrictOrderingFor(e => e.ComplexForms) // sorted alphabetically
                .WithoutStrictOrderingFor(e => e.Path.EndsWith($".{nameof(Sense.SemanticDomains)}")) // not sorted
                .For(e => e.Senses).Exclude(s => s.Order)
                .For(e => e.Components).Exclude(c => c.Order)
                .For(e => e.ComplexForms).Exclude(c => c.Order)
                .For(e => e.Senses).For(s => s.ExampleSentences).Exclude(e => e.Order);
            if (currentApiType == ApiType.Crdt)
            {
                // does not yet update Headwords 😕
                options = options
                    .For(e => e.Components).Exclude(c => c.ComplexFormHeadword)
                    .For(e => e.ComplexForms).Exclude(c => c.ComponentHeadword);
            }
            if (currentApiType == ApiType.FwData)
            {
                // does not support changing MorphType yet (see UpdateEntryProxy.MorphType)
                options = options.Excluding(e => e.MorphType);
            }
            return options;
        });
    }

    [Fact]
    public async Task NormalizesStringsToNFD()
    {
        // arrange
        var formC = "ймыл";
        var formD = "ймыл";

        formC.Should().NotBe(formD);
        formC.Should().Be(formC.Normalize(NormalizationForm.FormC));
        formD.Should().Be(formD.Normalize(NormalizationForm.FormD));
        formC.Normalize(NormalizationForm.FormD).Should().Be(formD);

        var entry1Id = Guid.NewGuid();
        await Api.CreateEntry(new() { Id = entry1Id });
        var entry1_before_formC = new Entry() { Id = entry1Id, LexemeForm = { { "en", formC } } };
        var entry1_after_formD = new Entry() { Id = entry1Id, LexemeForm = { { "en", formD } } };

        var entry2Id = Guid.NewGuid();
        var entry2_new_formC = new Entry() { Id = entry2Id, LexemeForm = { { "en", formC } } };

        // act
        await EntrySync.SyncWithoutComplexFormsAndComponents(
            [entry1_before_formC],
            [entry1_after_formD, entry2_new_formC], Api);

        // assert
        var entry1After = await Api.GetEntry(entry1Id);
        entry1After!.LexemeForm["en"].Should().Be(formD);
        // this fails for crdt - https://github.com/sillsdev/languageforge-lexbox/issues/2065
        // var entry2After = await Api.GetEntry(entry2Id);
        // entry2After!.LexemeForm["en"].Should().Be(formD);
    }

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
    public async Task SyncWithoutComplexFormsAndComponents_CorrectlySyncsUpdatedEntries()
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

        // act
        var (changes, added) = await EntrySync.SyncWithoutComplexFormsAndComponents([componentBefore], [componentAfter, complexForm], Api);
        added.Should().HaveCount(1);
        var addedComplexForm = added.First();

        // assert
        var actualComponent = await Api.GetEntry(componentAfter.Id);
        actualComponent.Should().BeEquivalentTo(componentAfter,
            options => options.Excluding(e => e.ComplexForms));
        actualComponent.ComplexForms.Should().BeEmpty();

        var actualComplexForm = await Api.GetEntry(complexForm.Id);
        addedComplexForm.Should().BeEquivalentTo(actualComplexForm);
        actualComplexForm.Should().BeEquivalentTo(complexForm,
            options => options.Excluding(e => e.Components));
        actualComplexForm.Components.Should().BeEmpty();
    }

    [Fact]
    public async Task SyncWithoutComplexFormsAndComponents_CorrectlySyncsAddedEntries()
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

        // act
        var (_, added) = await EntrySync.SyncWithoutComplexFormsAndComponents([], [component, complexForm], Api);
        added.Should().HaveCount(2);
        var addedComponent = added.ElementAt(0);
        var addedComplexForm = added.ElementAt(1);

        // assert
        var actualComponent = await Api.GetEntry(component.Id);
        addedComponent.Should().BeEquivalentTo(actualComponent);
        actualComponent.Should().BeEquivalentTo(component,
            options => options.Excluding(e => e.ComplexForms));
        actualComponent.ComplexForms.Should().BeEmpty();

        var actualComplexForm = await Api.GetEntry(complexForm.Id);
        addedComplexForm.Should().BeEquivalentTo(actualComplexForm);
        actualComplexForm.Should().BeEquivalentTo(complexForm,
            options => options.Excluding(e => e.Components));
        actualComplexForm.Components.Should().BeEmpty();
    }

    [Fact]
    public async Task SyncComplexFormsAndComponents_CorrectlySyncsUpdatedEntries()
    {
        // Arrange
        // - before
        var componentBefore = await Api.CreateEntry(new() { LexemeForm = { { "en", "component" } } });
        var complexFormBefore = await Api.CreateEntry(new() { LexemeForm = { { "en", "complex form" } } });

        // - after
        var componentAfter = componentBefore.Copy();
        componentAfter.LexemeForm["en"] = "component updated";
        var complexFormAfter = complexFormBefore.Copy();
        complexFormAfter.LexemeForm["en"] = "complex form updated";
        var complexFormComponent = ComplexFormComponent.FromEntries(complexFormAfter, componentAfter);
        componentAfter.ComplexForms.Add(complexFormComponent);
        complexFormAfter.Components.Add(complexFormComponent);

        // act
        await EntrySync.SyncComplexFormsAndComponents([componentBefore, complexFormBefore], [componentAfter, complexFormAfter], Api);

        // assert
        var actualComponent = await Api.GetEntry(componentAfter.Id);
        actualComponent.Should().NotBeNull();

        // complex forms were synced
        actualComponent.ComplexForms.Should().NotBeEmpty();
        actualComponent.ComplexForms.Should().BeEquivalentTo(componentAfter.ComplexForms, options
            => options.Excluding(c => c.Id)
                .Excluding(c => c.Order)
                // The lexeme-form/headword wasn't synced so it doesn't match the "after" version
                .Excluding(c => c.ComplexFormHeadword)
                .Excluding(c => c.ComponentHeadword));

        var actualComplexForm = await Api.GetEntry(complexFormAfter.Id);
        actualComplexForm.Should().NotBeNull();
        // components were synced
        actualComplexForm.Components.Should().NotBeEmpty();
        actualComplexForm.Components.Should().BeEquivalentTo(complexFormAfter.Components, options
            => options.Excluding(c => c.Id)
                .Excluding(c => c.Order)
                // The lexeme-form/headword wasn't synced so it doesn't match the "after" version
                .Excluding(c => c.ComplexFormHeadword)
                .Excluding(c => c.ComponentHeadword));

        // Lexeme form was not synced
        actualComponent.LexemeForm.Should().BeEquivalentTo(componentBefore.LexemeForm);
        actualComponent.LexemeForm.Should().NotBeEquivalentTo(componentAfter.LexemeForm);
        actualComplexForm.LexemeForm.Should().BeEquivalentTo(complexFormBefore.LexemeForm);
        actualComplexForm.LexemeForm.Should().NotBeEquivalentTo(complexFormAfter.LexemeForm);
    }

    [Fact]
    public async Task SyncComplexFormsAndComponents_ThrowsExceptionIfEntryNotInBefore()
    {
        // Arrange
        var component = new Entry() { Id = Guid.NewGuid() };
        var complexForm = new Entry() { Id = Guid.NewGuid() };
        var complexFormComponent = ComplexFormComponent.FromEntries(complexForm, component);
        component.ComplexForms.Add(complexFormComponent);
        complexForm.Components.Add(complexFormComponent);

        // Act
        var act = () => EntrySync.SyncComplexFormsAndComponents([], [component, complexForm], Api);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SyncComplexFormsAndComponents_ThrowsExceptionIfEntryNotInAfter()
    {
        // Arrange
        var component = new Entry() { Id = Guid.NewGuid() };
        var complexForm = new Entry() { Id = Guid.NewGuid() };
        var complexFormComponent = ComplexFormComponent.FromEntries(complexForm, component);
        component.ComplexForms.Add(complexFormComponent);
        complexForm.Components.Add(complexFormComponent);

        // Act
        var act = () => EntrySync.SyncComplexFormsAndComponents([component, complexForm], [], Api);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SyncComplexFormsAndComponents_MovesComponentsToCorrectPosition()
    {
        var componentA = await Api.CreateEntry(new() { LexemeForm = { { "en", "componentA" } } });
        var componentB = await Api.CreateEntry(new() { LexemeForm = { { "en", "componentB" } } });
        var complexFormId = Guid.NewGuid();
        var complexForm = await Api.CreateEntry(new()
        {
            Id = complexFormId,
            LexemeForm = { { "en", "complex form" } },
            Components =
            [
                new ComplexFormComponent()
                {
                    ComponentEntryId = componentA.Id,
                    ComponentHeadword = componentA.Headword(),
                    ComplexFormEntryId = complexFormId,
                    ComplexFormHeadword = "complex form",
                    Order = 1
                },
                new ComplexFormComponent()
                {
                    ComponentEntryId = componentB.Id,
                    ComponentHeadword = componentB.Headword(),
                    ComplexFormEntryId = complexFormId,
                    ComplexFormHeadword = "complex form",
                    Order = 2
                }
            ]
        });
        var complexFormAfter = complexForm.Copy();
        complexFormAfter.Components[0].Order = 3;
        complexFormAfter.Components = [..complexFormAfter.Components.Select(c =>
        {
            //simulate components coming from FLEx without an Id, VERY IMPORTANT!
            c.Id = Guid.Empty;
            return c;
        }).OrderBy(c => c.Order)];

        await EntrySync.SyncComplexFormsAndComponents([complexForm], [complexFormAfter], Api);

        var actual = await Api.GetEntry(complexFormId);
        actual.Should().NotBeNull();
        actual.Components.Should().HaveCount(2);
        actual.Components[0].ComponentHeadword.Should().Be("componentB");
        actual.Components[1].ComponentHeadword.Should().Be("componentA");
    }
}
