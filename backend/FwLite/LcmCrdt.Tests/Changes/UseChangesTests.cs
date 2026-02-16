using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Bogus;
using FluentAssertions.Execution;
using LcmCrdt.Changes;
using LcmCrdt.Changes.CustomJsonPatches;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Changes.ExampleSentences;
using MiniLcm.SyncHelpers;
using SIL.Harmony.Changes;
using SIL.Harmony.Resource;
using SystemTextJsonPatch;

namespace LcmCrdt.Tests.Changes;

public class UseChangesTests(MiniLcmApiFixture fixture) : IClassFixture<MiniLcmApiFixture>
{

    private static readonly Randomizer random = new();
    private static readonly Lazy<JsonSerializerOptions> LazyOptions = new(() =>
    {
        var options = TestJsonOptions.Harmony();
        options.ReadCommentHandling = JsonCommentHandling.Skip;
        return options;
    });
    private static readonly JsonSerializerOptions Options = LazyOptions.Value;

    [Fact]
    public async Task CanAddAllChangeTypes()
    {
        var shuffledChangesWithDependencies = random.Shuffle(GetAllChanges())
            .ToDictionary(change => change.Change, change => change.Dependencies == null ? null : random.Shuffle(change.Dependencies).ToList());
        var pendingChanges = shuffledChangesWithDependencies.Select(c => c.Key).ToList();
        var queuedChanges = new List<IChange>(pendingChanges.Count);

        while (pendingChanges is not [])
        {
            var change = FindFirstSatisfiedChangeRecursive(pendingChanges, shuffledChangesWithDependencies, queuedChanges);
            if (!pendingChanges.Remove(change)) throw new InvalidOperationException("Change not found in pending changes");
            queuedChanges.Add(change);
        }

        try
        {
            await fixture.DataModel.AddChanges(Guid.NewGuid(), queuedChanges);
        }
        catch (Exception e)
        {
            var serializedQueuedChanges = JsonSerializer.Serialize(queuedChanges, Options);
            throw new Exception($"Failed to add changes: {e.Message}. JSON:\n\n{serializedQueuedChanges}", e);
        }
    }

    [Fact]
    public async Task CanSyncAllChangesWithDuplicates()
    {
        var shuffledChangesWithDependencies = random.Shuffle(GetAllChanges())
            .ToDictionary(change => change.Change, change => change.Dependencies == null ? null : random.Shuffle(change.Dependencies).ToList());
        var pendingChanges = shuffledChangesWithDependencies.Select(c => c.Key).ToList();
        var committedChanges = new List<IChange>(pendingChanges.Count);

        while (pendingChanges is not [])
        {
            var change = FindFirstSatisfiedChangeRecursive(pendingChanges, shuffledChangesWithDependencies, committedChanges);
            if (!pendingChanges.Remove(change)) throw new InvalidOperationException("Change not found in pending changes");

            await fixture.DataModel.AddChange(Guid.NewGuid(), change);

            // Add a duplicate change
            var duplicateChange = JsonSerializer.Deserialize(
                JsonSerializer.Serialize(change, Options),
                change.GetType()) as IChange;
            duplicateChange.Should().NotBeNull();
            duplicateChange.GetType().Should().Be(change.GetType());
            await fixture.DataModel.AddChange(Guid.NewGuid(), duplicateChange);

            if (change.SupportsNewEntity())
            {
                // The previous duplicate change is presumably a no-op, so we'll make a duplicate entity with a different ID as well.
                var duplicateCreateChange = JsonSerializer.Deserialize(
                    JsonSerializer.Serialize(change, Options),
                    change.GetType()) as IChange;
                duplicateCreateChange.Should().NotBeNull();
                duplicateCreateChange.EntityId = Guid.NewGuid();
                duplicateCreateChange.GetType().Should().Be(change.GetType());
                await fixture.DataModel.AddChange(Guid.NewGuid(), duplicateCreateChange);
            }

            var allEntries = await fixture.Api.GetEntries().ToArrayAsync();
            var result = await EntrySync.SyncFull(allEntries, allEntries, fixture.Api);
            result.Should().Be(0);

            committedChanges.Add(change);
        }
    }

    private bool AreSatisfied([NotNullWhen(false)] IEnumerable<IChange>? dependencies, IEnumerable<IChange> appliedChanges)
    {
        return dependencies == null || dependencies.All(appliedChanges.Contains);
    }

    private IChange FindFirstSatisfiedChangeRecursive(List<IChange> changes,
        Dictionary<IChange, List<IChange>?> allChangesWithDependencies,
        List<IChange> appliedChanges)
    {
        var change = changes.First(d => !appliedChanges.Contains(d));
        var dependencies = allChangesWithDependencies[change];
        if (AreSatisfied(dependencies, appliedChanges))
        {
            return change;
        }
        else
        {
            return FindFirstSatisfiedChangeRecursive(dependencies, allChangesWithDependencies, appliedChanges);
        }
    }

    [Fact]
    public void ChangesIncludeAllExpectedChangeTypes()
    {
        var allExpectedChangeTypes = LcmCrdtKernel.AllChangeTypes()
            .Where(type => !type.Name.StartsWith("DeleteChange`") && !type.Name.StartsWith("JsonPatchChange`")).ToArray();
        allExpectedChangeTypes.Should().NotBeEmpty();

        var testedTypes = GetAllChanges().Select(c => c.Change.GetType()).ToArray();
        using (new AssertionScope())
        {
            foreach (var allChangeType in allExpectedChangeTypes)
            {
                testedTypes.Should().Contain(allChangeType);
            }
        }
    }

    private record ChangeWithDependencies(IChange Change, IEnumerable<IChange>? Dependencies = null);

    private static IEnumerable<ChangeWithDependencies> GetAllChanges()
    {
        var entry = new Entry { Id = Guid.NewGuid(), LexemeForm = { { "en", "test entry" } } };
        var createEntryChange = new CreateEntryChange(entry);
        yield return new ChangeWithDependencies(createEntryChange);

        var partOfSpeech = new PartOfSpeech { Id = Guid.NewGuid(), Name = { { "en", "test pos" } } };
        var createPartOfSpeechChange = new CreatePartOfSpeechChange(partOfSpeech.Id, partOfSpeech.Name);
        yield return new ChangeWithDependencies(createPartOfSpeechChange);

        var sense = new Sense { Id = Guid.NewGuid(), Gloss = { { "en", "test sense" } } };
        var createSenseChange = new CreateSenseChange(sense, entry.Id);
        yield return new ChangeWithDependencies(createSenseChange, [createEntryChange]);

        var exampleSentence = new ExampleSentence { Id = Guid.NewGuid(), Sentence = new() { { "en", new RichString("test sentence") } } };
        var createExampleSentenceChange = new CreateExampleSentenceChange(exampleSentence, sense.Id);
        yield return new ChangeWithDependencies(createExampleSentenceChange, [createSenseChange]);

        var jsonPatchExampleSentenceChange = new JsonPatchExampleSentenceChange(
            exampleSentence.Id,
            new JsonPatchDocument<ExampleSentence>()
                .Replace(sentence => sentence.Reference, new RichString("hello", "en"))
            );
        yield return new ChangeWithDependencies(jsonPatchExampleSentenceChange, [createExampleSentenceChange]);

        var translation = new Translation { Id = Guid.NewGuid(), Text = new() { { "en", new RichString("test translation") } } };
        var createTranslationChange = new AddTranslationChange(exampleSentence.Id, translation);
        yield return new ChangeWithDependencies(createTranslationChange, [createExampleSentenceChange]);

        var updateTranslationChange = new UpdateTranslationChange(exampleSentence.Id, translation.Id,
        new JsonPatchDocument<Translation>()
            .Replace(sentence => sentence.Text, new() { { "en", new RichString("test translation update") } }));
        yield return new ChangeWithDependencies(updateTranslationChange, [createTranslationChange]);

        var setFirstTranslationIdChange = new SetFirstTranslationIdChange(exampleSentence.Id, Guid.NewGuid());
        yield return new ChangeWithDependencies(setFirstTranslationIdChange, [createExampleSentenceChange]);

        var removeTranslationChange = new RemoveTranslationChange(exampleSentence.Id, translation.Id);
        yield return new ChangeWithDependencies(removeTranslationChange, [createTranslationChange]);

        var semanticDomain = new SemanticDomain { Id = Guid.NewGuid(), Name = { { "en", "test sd" } } };
        var createSemanticDomainChange = new CreateSemanticDomainChange(semanticDomain.Id, semanticDomain.Name, "1.1.1");
        yield return new ChangeWithDependencies(createSemanticDomainChange);

        var writingSystem = new WritingSystem { Id = Guid.NewGuid(), WsId = "de", Name = "test ws", Abbreviation = "tws", Font = "Arial", Type = WritingSystemType.Vernacular };
        var createWritingSystemChange = new CreateWritingSystemChange(writingSystem, writingSystem.Id, 0);
        yield return new ChangeWithDependencies(createWritingSystemChange);

        var complexFormTypeName = new MultiString { { "en", "test cft" } };
        var complexFormType = new ComplexFormType { Id = Guid.NewGuid(), Name = complexFormTypeName };
        var createComplexFormType = new CreateComplexFormType(complexFormType.Id, complexFormTypeName);
        yield return new ChangeWithDependencies(createComplexFormType);

        var complexFormEntry = new Entry { Id = Guid.NewGuid(), LexemeForm = { { "en", "test complex form" } } };
        var createComplexFormEntryChange = new CreateEntryChange(complexFormEntry);
        yield return new ChangeWithDependencies(createComplexFormEntryChange);

        var complexFormComponent = ComplexFormComponent.FromEntries(complexFormEntry, entry, sense.Id);
        var createComplexFormComponentChange = new AddEntryComponentChange(complexFormComponent);
        yield return new ChangeWithDependencies(createComplexFormComponentChange, [createComplexFormEntryChange, createEntryChange, createSenseChange]);

        var setPartOfSpeechChange = new SetPartOfSpeechChange(sense.Id, partOfSpeech.Id);
        yield return new ChangeWithDependencies(setPartOfSpeechChange, [createSenseChange, createPartOfSpeechChange]);

        var addSemanticDomainChange = new AddSemanticDomainChange(semanticDomain, sense.Id);
        yield return new ChangeWithDependencies(addSemanticDomainChange, [createSenseChange, createSemanticDomainChange]);

        var semanticDomain2 = new SemanticDomain { Id = Guid.NewGuid(), Name = { { "en", "sd 2" } } };
        var addSemanticDomain2Change = new CreateSemanticDomainChange(semanticDomain2.Id, semanticDomain2.Name, "1.1.2");
        yield return new ChangeWithDependencies(addSemanticDomain2Change);

        var replaceSemanticDomainChange = new ReplaceSemanticDomainChange(semanticDomain.Id, semanticDomain2, sense.Id);
        yield return new ChangeWithDependencies(replaceSemanticDomainChange, [addSemanticDomain2Change, addSemanticDomainChange]);

        var removeSemanticDomainChange = new RemoveSemanticDomainChange(semanticDomain2.Id, sense.Id);
        yield return new ChangeWithDependencies(removeSemanticDomainChange, [replaceSemanticDomainChange]);

        var addComplexFormTypeChange = new AddComplexFormTypeChange(entry.Id, complexFormType);
        yield return new ChangeWithDependencies(addComplexFormTypeChange, [createComplexFormType, createEntryChange]);

        var removeComplexFormTypeChange = new RemoveComplexFormTypeChange(entry.Id, complexFormType.Id);
        yield return new ChangeWithDependencies(removeComplexFormTypeChange, [addComplexFormTypeChange]);

        var componentEntry = new Entry { Id = Guid.NewGuid(), LexemeForm = { { "en", "test component" } } };
        var createcomponentEntryChange = new CreateEntryChange(componentEntry);
        yield return new ChangeWithDependencies(createcomponentEntryChange);

        var setComplexFormComponentChange = SetComplexFormComponentChange.NewComponent(complexFormComponent.Id, componentEntry.Id);
        yield return new ChangeWithDependencies(setComplexFormComponentChange, [createcomponentEntryChange, createComplexFormComponentChange]);

        var setSenseOrderChange = new LcmCrdt.Changes.SetOrderChange<Sense>(sense.Id, 10);
        yield return new ChangeWithDependencies(setSenseOrderChange, [createSenseChange]);

        var setExampleSentenceOrderChange = new LcmCrdt.Changes.SetOrderChange<ExampleSentence>(exampleSentence.Id, 10);
        yield return new ChangeWithDependencies(setExampleSentenceOrderChange, [createExampleSentenceChange]);

        var setComplexFormComponentOrderChange = new LcmCrdt.Changes.SetOrderChange<ComplexFormComponent>(complexFormComponent.Id, 10);
        yield return new ChangeWithDependencies(setComplexFormComponentOrderChange, [createComplexFormComponentChange]);

        var setWritingSystemOrderChange = new LcmCrdt.Changes.SetOrderChange<WritingSystem>(writingSystem.Id, 10);
        yield return new ChangeWithDependencies(setWritingSystemOrderChange, [createWritingSystemChange]);

        var publication = new Publication { Id = Guid.NewGuid(), Name = { { "en", "Main" } } };
        var createPublicationChange = new CreatePublicationChange(publication.Id, publication.Name);
        yield return new ChangeWithDependencies(createPublicationChange);

        var publication2 = new Publication { Id = Guid.NewGuid(), Name = { { "en", "Second" } } };
        var createPublication2Change = new CreatePublicationChange(publication2.Id, publication2.Name);
        yield return new ChangeWithDependencies(createPublication2Change);

        var addPublicationChange = new AddPublicationChange(entry.Id, publication);
        yield return new ChangeWithDependencies(addPublicationChange, [createPublicationChange, createEntryChange]);

        var replacePublicationChange = new ReplacePublicationChange(entry.Id, publication2, publication.Id);
        yield return new ChangeWithDependencies(replacePublicationChange, [createPublicationChange, addPublicationChange, createPublication2Change]);

        var removePublicationChange = new RemovePublicationChange(entry.Id, publication2.Id);
        yield return new ChangeWithDependencies(removePublicationChange, [replacePublicationChange]);

        yield return new ChangeWithDependencies(new CreateRemoteResourceChange(Guid.NewGuid(), "test-remote-id"));
        var createRemoteResourcePendingUploadChange = new CreateRemoteResourcePendingUploadChange(Guid.NewGuid());
        yield return new ChangeWithDependencies(createRemoteResourcePendingUploadChange);
        yield return new ChangeWithDependencies(
            new RemoteResourceUploadedChange(createRemoteResourcePendingUploadChange.EntityId, "test-remote-id"),
            [createRemoteResourcePendingUploadChange]);

        var customView = new CustomView
        {
            Id = Guid.NewGuid(),
            Name = "Test View",
            Base = ViewBase.FwLite,
            EntryFields = [new ViewField { FieldId = "lexemeForm" }],
            SenseFields = [new ViewField { FieldId = "gloss" }],
            ExampleFields = [new ViewField { FieldId = "sentence" }],
            Vernacular = [new WritingSystemId("en")],
            Analysis = [new WritingSystemId("en")],
        };
        var createCustomViewChange = new CreateCustomViewChange(
            customView.Id,
            customView
        );
        yield return new ChangeWithDependencies(createCustomViewChange);
        var editCustomViewChange = new EditCustomViewChange(
            customView.Id,
            customView with
            {
                Name = "Updated View",
                Base = ViewBase.FieldWorks,
                EntryFields = [new ViewField { FieldId = "citationForm" }],
                SenseFields = [new ViewField { FieldId = "definition" }],
                ExampleFields = [new ViewField { FieldId = "translations" }],
                Vernacular = [new WritingSystemId("fr")],
                Analysis = [new WritingSystemId("en")]
            });
        yield return new ChangeWithDependencies(editCustomViewChange, [createCustomViewChange]);
    }
}
