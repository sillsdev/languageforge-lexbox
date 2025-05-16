using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Bogus;
using FluentAssertions.Execution;
using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using SIL.Harmony.Changes;

namespace LcmCrdt.Tests.Changes;

public class UseChangesTests(MiniLcmApiFixture fixture) : IClassFixture<MiniLcmApiFixture>
{

    private static readonly Randomizer random = new();
    private static readonly Lazy<JsonSerializerOptions> LazyOptions = new(() =>
    {
        var config = new CrdtConfig();
        LcmCrdtKernel.ConfigureCrdt(config);
        config.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
        return config.JsonSerializerOptions;
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

    private bool AreSatisfied([NotNullWhen(false)] IEnumerable<IChange>? dependencies, IEnumerable<IChange> queuedChanges)
    {
        return dependencies == null || dependencies.All(queuedChanges.Contains);
    }

    private IChange FindFirstSatisfiedChangeRecursive(List<IChange> changes,
        Dictionary<IChange, List<IChange>?> allChangesWithDependencies,
        List<IChange> queuedChanges)
    {
        var change = changes.First(d => !queuedChanges.Contains(d));
        var dependencies = allChangesWithDependencies[change];
        if (AreSatisfied(dependencies, queuedChanges))
        {
            return change;
        }
        else
        {
            return FindFirstSatisfiedChangeRecursive(dependencies, allChangesWithDependencies, queuedChanges);
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

        var exampleSentence = new ExampleSentence { Id = Guid.NewGuid(), Sentence = new() { { "en", "test sentence" } } };
        var createExampleSentenceChange = new CreateExampleSentenceChange(exampleSentence, sense.Id);
        yield return new ChangeWithDependencies(createExampleSentenceChange, [createSenseChange]);

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

        var createPublicationChange = new CreatePublicationChange(Guid.NewGuid(), new (){{"en", "Main"}});
        yield return new ChangeWithDependencies(createPublicationChange);
    }
}
