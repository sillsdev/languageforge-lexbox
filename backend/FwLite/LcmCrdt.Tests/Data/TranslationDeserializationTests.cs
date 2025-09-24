using System.Text.Json;
using LcmCrdt.Objects;

namespace LcmCrdt.Tests.Data;

public class TranslationDeserializationTests
{
    private readonly JsonSerializerOptions _options = TestJsonOptions.Harmony();

    public const string Example = """
                                  {
                                    "$type": "MiniLcmCrdtAdapter",
                                    "Obj": {
                                      "$type": "ExampleSentence",
                                      "Id": "135d7c84-95d8-4707-a400-e1f3619d90fb",
                                      "Order": 1,
                                      "Sentence": {
                                        "en": {
                                          "Spans": [
                                            {
                                              "Text": "We ate an apple",
                                              "Ws": "en"
                                            }
                                          ]
                                        },
                                        "de": {
                                          "Spans": [
                                            {
                                              "Text": "de",
                                              "Ws": "de"
                                            }
                                          ]
                                        }
                                      },
                                      "Translation": {
                                        "en": {
                                          "Spans": [
                                            {
                                              "Text": "en",
                                              "Ws": "en"
                                            }
                                          ]
                                        }
                                      },
                                      "Reference": {
                                        "Spans": [
                                          {
                                            "Text": "ref",
                                            "Ws": "en"
                                          }
                                        ]
                                      },
                                      "SenseId": "b9440091-a9fc-4769-82b1-2ee8d030808d",
                                      "DeletedAt": null
                                    },
                                    "Id": "135d7c84-95d8-4707-a400-e1f3619d90fb",
                                    "DeletedAt": null
                                  }
                                  """;

    [Fact]
    public void CanDeserializeFromExampleSentence()
    {
        var adapter = JsonSerializer.Deserialize<MiniLcmCrdtAdapter>(Example, _options);
        var example = adapter?.DbObject.Should().BeOfType<ExampleSentence>().Subject;
        example.Should().NotBeNull();
        example.Id.Should().Be("135d7c84-95d8-4707-a400-e1f3619d90fb");
        example.Order.Should().Be(1);
        example.Sentence.Should().NotBeNull();
        example.Reference.Should().NotBeNull();
        example.SenseId.Should().Be("b9440091-a9fc-4769-82b1-2ee8d030808d");
        var translation = example.Translations.Should().ContainSingle().Subject;
        translation.Text["en"].Should().BeEquivalentTo(new RichString("en", "en"));
    }

    [Fact]
    public void CanDeserializeRichTextToTranslation()
    {
        //this simulates when EF core renames the column and tries to write data. There's regression tests which cover this also but this is simpler to test.
        var json = """
                    {
                      "en": {
                        "Spans": [
                          {
                            "Text": "en",
                            "Ws": "en"
                          }
                        ]
                      }
                    }
                   """;
        var translations = JsonSerializer.Deserialize<DbTranslationDeserializationTarget>(json, _options)?.GetTranslations();
        var translation = translations.Should().ContainSingle().Subject;
        translation.Text["en"].Should().BeEquivalentTo(new RichString("en", "en"));
    }
}
