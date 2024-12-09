using System.Text.Json;
using FluentAssertions.Execution;
using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using SIL.Harmony.Changes;
using SystemTextJsonPatch;

namespace LcmCrdt.Tests.Changes;

public class ChangeSerializationTests
{
    public static IEnumerable<object[]> Changes()
    {
        yield return
        [
            new AddEntryComponentChange(ComplexFormComponent.FromEntries(
                new Entry() { Id = Guid.NewGuid() },
                new Entry() { Id = Guid.NewGuid() }))
        ];
        yield return
        [
            new CreateEntryChange(new Entry() { Id = Guid.NewGuid() })
        ];
        yield return [
            new CreateSenseChange(new Sense() { Id = Guid.NewGuid() }, Guid.NewGuid())
        ];
        yield return [
            new CreateExampleSentenceChange(new ExampleSentence() { Id = Guid.NewGuid() }, Guid.NewGuid())
        ];
        yield return
        [
            new CreatePartOfSpeechChange(Guid.NewGuid(), new MultiString())
        ];
        yield return
        [
            new CreateSemanticDomainChange(Guid.NewGuid(), new MultiString(), "1")
        ];
        yield return
        [
            new CreateWritingSystemChange(new WritingSystem()
            {
                Id = Guid.NewGuid(),
                WsId = "en",
                Name = "en",
                Abbreviation = "en",
                Font = "Arial",
                Exemplars = ["en"],
                Type = WritingSystemType.Analysis,
                Order = 1,
            }, WritingSystemType.Analysis, Guid.NewGuid(), 1)
        ];
        yield return [
            new SetPartOfSpeechChange(Guid.NewGuid(), Guid.NewGuid())
        ];
        yield return [new AddSemanticDomainChange(new SemanticDomain() { Id = Guid.NewGuid() }, Guid.NewGuid())];
        yield return [new RemoveSemanticDomainChange(Guid.NewGuid(), Guid.NewGuid())];
        yield return [new ReplaceSemanticDomainChange(Guid.NewGuid(), new SemanticDomain() { Id = Guid.NewGuid() }, Guid.NewGuid())];
        yield return [new RemoveComplexFormTypeChange(Guid.NewGuid(), Guid.NewGuid())];
        yield return [new CreateComplexFormType(Guid.NewGuid(), new MultiString())];
        yield return [new AddComplexFormTypeChange(Guid.NewGuid(), new() { Id = Guid.NewGuid(), Name = new()})];
        yield return [SetComplexFormComponentChange.NewComplexForm(Guid.NewGuid(), Guid.NewGuid())];
        yield return [SetComplexFormComponentChange.NewComponent(Guid.NewGuid(), Guid.NewGuid())];
        yield return [SetComplexFormComponentChange.NewComponentSense(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())];

        yield return [new JsonPatchChange<Entry>(Guid.NewGuid(), new JsonPatchDocument<Entry>())];
        yield return [new JsonPatchChange<Sense>(Guid.NewGuid(), new JsonPatchDocument<Sense>())];
        yield return [new JsonPatchChange<ExampleSentence>(Guid.NewGuid(), new JsonPatchDocument<ExampleSentence>())];
        yield return [new JsonPatchChange<WritingSystem>(Guid.NewGuid(), new JsonPatchDocument<WritingSystem>())];
        yield return [new JsonPatchChange<PartOfSpeech>(Guid.NewGuid(), new JsonPatchDocument<PartOfSpeech>())];
        yield return [new JsonPatchChange<SemanticDomain>(Guid.NewGuid(), new JsonPatchDocument<SemanticDomain>())];
        yield return [new JsonPatchChange<ComplexFormType>(Guid.NewGuid(), new JsonPatchDocument<ComplexFormType>())];
        yield return [new DeleteChange<Entry>(Guid.NewGuid())];
        yield return [new DeleteChange<Sense>(Guid.NewGuid())];
        yield return [new DeleteChange<ExampleSentence>(Guid.NewGuid())];
        yield return [new DeleteChange<WritingSystem>(Guid.NewGuid())];
        yield return [new DeleteChange<PartOfSpeech>(Guid.NewGuid())];
        yield return [new DeleteChange<SemanticDomain>(Guid.NewGuid())];
        yield return [new DeleteChange<ComplexFormType>(Guid.NewGuid())];
        yield return [new DeleteChange<ComplexFormComponent>(Guid.NewGuid())];
    }

    [Theory]
    [MemberData(nameof(Changes))]
    public void CanRoundTripChanges(IChange change)
    {
        var config = new CrdtConfig();
        LcmCrdtKernel.ConfigureCrdt(config);
        var type = change.GetType();
        var json = JsonSerializer.Serialize(change, config.JsonSerializerOptions);
        var newChange = JsonSerializer.Deserialize(json, type, config.JsonSerializerOptions);
        newChange.Should().BeEquivalentTo(change);
    }

    [Fact]
    public void ChangesIncludesAllValidChangeTypes()
    {
        var allChangeTypes = LcmCrdtKernel.AllChangeTypes();
        allChangeTypes.Should().NotBeEmpty();
        var testedTypes = Changes().Select(c => c[0].GetType()).ToArray();
        using (new AssertionScope())
        {
            foreach (var allChangeType in allChangeTypes)
            {
                testedTypes.Should().Contain(allChangeType);
            }
        }
    }
}
