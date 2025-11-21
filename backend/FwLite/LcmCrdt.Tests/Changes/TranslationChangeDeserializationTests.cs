using System.Text.Json;
using LcmCrdt.Changes;
using LcmCrdt.Objects;
using Moq;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;

namespace LcmCrdt.Tests.Changes;

public class TranslationChangeDeserializationTests
{
    private readonly JsonSerializerOptions _options = TestJsonOptions.Harmony();

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
    public async Task CanDeserializeCreateExampleJson()
    {
        // arrange
        var change = JsonSerializer.Deserialize<CreateExampleSentenceChange>(CreateExampleJson, _options);
        change.Should().NotBeNull();
        var mockContext = new Mock<IChangeContext>(MockBehavior.Strict);
        mockContext.Setup(c => c.IsObjectDeleted(It.IsAny<Guid>())).ReturnsAsync(false);

        // act
        var adapter = await change.NewEntity(null!, mockContext.Object);

        // assert
        var exampleSentence = adapter.Should().BeOfType<ExampleSentence>().Subject;
        var translation = exampleSentence.Translations.Should().ContainSingle().Subject;
        translation.Text["en"].Should().BeEquivalentTo(new RichString("test", "en"));
    }

    private const string JsonPatchTranslationAdd = /*lang=json,strict*/ """
{
  "$type": "jsonPatch:ExampleSentence",
  "PatchDocument": [
    {
      "op": "add",
      "path": "/Translation/en",
      "value": {
        "Spans": [
          {
            "Text": "updated",
            "Ws": "en"
          }
        ]
      }
    }
  ],
  "EntityId": "135d7c84-95d8-4707-a400-e1f3619d90fb"
}
""";

    private const string JsonPatchTranslationReplace = /*lang=json,strict*/ """
{
  "$type": "jsonPatch:ExampleSentence",
  "PatchDocument": [
    {
      "op": "replace",
      "path": "/Translation/en",
      "value": {
        "Spans": [
          {
            "Text": "updated",
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
    [InlineData(JsonPatchTranslationAdd)]
    [InlineData(JsonPatchTranslationReplace)]
    public async Task CanDeserializeUpdateJsonPatchTranslation(string json)
    {
        var change = JsonSerializer.Deserialize<IChange>(json, _options);
        change.Should().NotBeNull();
        var exampleSentence = new ExampleSentence()
        {
            Translations = [
            new Translation()
            {
                Id = Guid.NewGuid(),
                Text = { { "en", new RichString("old", "en") } }
            }]
        };
        await change.ApplyChange(new MiniLcmCrdtAdapter(exampleSentence), null!);
        var translation = exampleSentence.Translations.Should().ContainSingle().Subject;
        translation.Text["en"].Should().BeEquivalentTo(new RichString("updated", "en"));
        translation.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(JsonPatchTranslationAdd)]
    [InlineData(JsonPatchTranslationReplace)]
    public async Task CanDeserializeUpdateJsonPatchTranslation_NoStartingTranslation(string json)
    {
        var change = JsonSerializer.Deserialize<IChange>(json, _options);
        change.Should().NotBeNull();
        var exampleSentence = new ExampleSentence()
        {
            Translations = []
        };
        await change.ApplyChange(new MiniLcmCrdtAdapter(exampleSentence), null!);
        var translation = exampleSentence.Translations.Should().ContainSingle().Subject;
        translation.Text["en"].Should().BeEquivalentTo(new RichString("updated", "en"));
        translation.Id.Should().NotBeEmpty();
    }

    private const string JsonPatchTranslationRemove = /*lang=json,strict*/ """
{
  "$type": "jsonPatch:ExampleSentence",
  "PatchDocument": [
    {
      "op": "remove",
      "path": "/Translation/en"
    }
  ],
  "EntityId": "135d7c84-95d8-4707-a400-e1f3619d90fb"
}
""";

    [Fact]
    public async Task CanDeserializeRemoveJsonPatchTranslation()
    {
        var change = JsonSerializer.Deserialize<IChange>(JsonPatchTranslationRemove, _options);
        change.Should().NotBeNull();
        var exampleSentence = new ExampleSentence()
        {
            Translations = [
            new Translation()
            {
                Text = {
                    { "es", new RichString("old", "es") },
                    { "en", new RichString("old", "en") },
                },
            }]
        };
        await change.ApplyChange(new MiniLcmCrdtAdapter(exampleSentence), null!);
        exampleSentence.Translations.Should().ContainSingle().Which
            .Text.Should().BeEquivalentTo(new RichMultiString() { { "es", new RichString("old", "es") } });
    }

    [Fact]
    public async Task CanDeserializeRemoveJsonPatchTranslation_NoStartingTranslation()
    {
        var change = JsonSerializer.Deserialize<IChange>(JsonPatchTranslationRemove, _options);
        change.Should().NotBeNull();
        var exampleSentence = new ExampleSentence()
        {
            Translations = []
        };
        await change.ApplyChange(new MiniLcmCrdtAdapter(exampleSentence), null!);
        exampleSentence.Translations.Should().BeEmpty();
    }
}
