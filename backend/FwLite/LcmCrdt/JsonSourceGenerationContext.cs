using System.Text.Json.Serialization;
using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Objects;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Db;

namespace LcmCrdt;

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(WritingSystem))]
[JsonSerializable(typeof(MiniLcmCrdtAdapter))]
[JsonSerializable(typeof(IObjectWithId))]
[JsonSerializable(typeof(IObjectBase))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Commit))]
[JsonSerializable(typeof(ObjectSnapshot))]
[JsonSerializable(typeof(Entry))]
[JsonSerializable(typeof(List<SemanticDomain>))]
[JsonSerializable(typeof(IList<SemanticDomain>))]
[JsonSerializable(typeof(List<ComplexFormType>))]
[JsonSerializable(typeof(MultiString))]
[JsonSerializable(typeof(IDictionary<WritingSystemId, string>))]
[JsonSerializable(typeof(Dictionary<WritingSystemId, string>))]
[JsonSerializable(typeof(List<IChange>))]

[JsonSerializable(typeof(CreateEntryChange))]
[JsonSerializable(typeof(CreateWritingSystemChange))]
[JsonSerializable(typeof(JsonPatchChange<Entry>))]
[JsonSerializable(typeof(JsonPatchChange<Sense>))]
[JsonSerializable(typeof(JsonPatchChange<ExampleSentence>))]
[JsonSerializable(typeof(JsonPatchChange<WritingSystem>))]
[JsonSerializable(typeof(JsonPatchChange<PartOfSpeech>))]
[JsonSerializable(typeof(JsonPatchChange<SemanticDomain>))]
[JsonSerializable(typeof(JsonPatchChange<ComplexFormType>))]
[JsonSerializable(typeof(JsonPatchChange<Publication>))]
[JsonSerializable(typeof(DeleteChange<Entry>))]
[JsonSerializable(typeof(DeleteChange<Sense>))]
[JsonSerializable(typeof(DeleteChange<ExampleSentence>))]
[JsonSerializable(typeof(DeleteChange<WritingSystem>))]
[JsonSerializable(typeof(DeleteChange<PartOfSpeech>))]
[JsonSerializable(typeof(DeleteChange<SemanticDomain>))]
[JsonSerializable(typeof(DeleteChange<ComplexFormType>))]
[JsonSerializable(typeof(DeleteChange<ComplexFormComponent>))]
[JsonSerializable(typeof(DeleteChange<Publication>))]
[JsonSerializable(typeof(SetPartOfSpeechChange))]
[JsonSerializable(typeof(AddSemanticDomainChange))]
[JsonSerializable(typeof(RemoveSemanticDomainChange))]
[JsonSerializable(typeof(ReplaceSemanticDomainChange))]
[JsonSerializable(typeof(CreateEntryChange))]
[JsonSerializable(typeof(CreateSenseChange))]
[JsonSerializable(typeof(CreateExampleSentenceChange))]
[JsonSerializable(typeof(CreatePartOfSpeechChange))]
[JsonSerializable(typeof(CreateSemanticDomainChange))]
[JsonSerializable(typeof(CreateWritingSystemChange))]
[JsonSerializable(typeof(CreatePublicationChange))]
[JsonSerializable(typeof(AddComplexFormTypeChange))]
[JsonSerializable(typeof(AddEntryComponentChange))]
[JsonSerializable(typeof(RemoveComplexFormTypeChange))]
[JsonSerializable(typeof(SetComplexFormComponentChange))]
[JsonSerializable(typeof(CreateComplexFormType))]
[JsonSerializable(typeof(Changes.SetOrderChange<Sense>))]
[JsonSerializable(typeof(Changes.SetOrderChange<ExampleSentence>))]
[JsonSerializable(typeof(Changes.SetOrderChange<ComplexFormComponent>))]
public partial class JsonSourceGenerationContext: JsonSerializerContext
{

}
