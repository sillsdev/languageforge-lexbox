using System.Text.Json;
using LcmCrdt.Changes;
using LcmCrdt.Objects;
using SIL.Harmony.Changes;

namespace LcmCrdt.Tests.Changes;

public class TranslationChangeDeserializationTests
{
    private readonly JsonSerializerOptions _options = TestJsonOptions.Default();

    private const string CreateExampleJson = """
 {
   "$type": "CreateExampleSentenceChange",
   "Order": 1,
   "SenseId": "e9188e00-7088-48e3-84e8-526583e5d6b3",
   "EntityId": "f5db4283-2217-4123-8d5e-527d4c51adbe",
   "Sentence": {
     "en": {
       "Spans": [
         {
           "Text": "test",
           "Ws": "en"
         }
       ]
     }
   },
   "Reference": {
     "Spans": []
   },
   "Translation": {
     "en": {
       "Spans": [
         {
           "Text": "test",
           "Ws": "en"
         }
       ]
     }
   }
 }
 """;

    [Fact]
    public void CanDeserializeCreateExampleJson()
    {
        var change = JsonSerializer.Deserialize<IChange>(CreateExampleJson, _options);
        change.Should().NotBeNull();
        var exampleSentenceChange = change.Should().BeOfType<CreateExampleSentenceChange>().Subject;
        var translation = exampleSentenceChange.Translations.Should().ContainSingle().Subject;
        translation.Text["en"].Should().BeEquivalentTo(new RichString("test", "en"));
    }

    private const string JsonPatchTranslation = """
{
  "$type": "jsonPatch:ExampleSentence",
  "PatchDocument": [
    {
      "op": "add",
      "path": "/Translation/en",
      "value": {
        "Spans": [
          {
            "Text": "en",
            "Ws": "en"
          }
        ]
      }
    }
  ],
  "EntityId": "135d7c84-95d8-4707-a400-e1f3619d90fb"
}
""";

    private const string JsonPatchTranslationReplace = """
{
  "$type": "jsonPatch:ExampleSentence",
  "PatchDocument": [
    {
      "op": "replace",
      "path": "/Translation/en",
      "value": {
        "Spans": [
          {
            "Text": "en",
            "Ws": "en"
          }
        ]
      }
    }
  ],
  "EntityId": "135d7c84-95d8-4707-a400-e1f3619d90fb"
}
""";


    [Theory]
    //will not work with add because there's already a translation there
    [InlineData(JsonPatchTranslationReplace)]
    public async Task CanDeserializeJsonPatchTranslation(string json)
    {
        var change = JsonSerializer.Deserialize<IChange>(json, _options);
        change.Should().NotBeNull();
        var exampleSentence = new ExampleSentence()
        {
            Translations = [
            new Translation()
            {
                Text = { { "en", new RichString("old", "en") } }
            }]
        };
        await change.ApplyChange(new MiniLcmCrdtAdapter(exampleSentence), null!);
        exampleSentence.Translations.Should().ContainSingle().Which.Text["en"].Should().BeEquivalentTo(new RichString("en", "en"));
    }

    [Theory]
    [InlineData(JsonPatchTranslation)]
    public async Task CanDeserializeJsonPatchTranslation_NoStartingTranslation(string json)
    {
        var change = JsonSerializer.Deserialize<IChange>(json, _options);
        change.Should().NotBeNull();
        var exampleSentence = new ExampleSentence()
        {
            Translations = []
        };
        await change.ApplyChange(new MiniLcmCrdtAdapter(exampleSentence), null!);
        exampleSentence.Translations.Should().ContainSingle().Which.Text["en"].Should().BeEquivalentTo(new RichString("en", "en"));
    }
}
