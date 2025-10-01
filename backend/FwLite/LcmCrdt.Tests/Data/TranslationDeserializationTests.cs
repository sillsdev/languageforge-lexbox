using System.Text.Json;
using LcmCrdt.Objects;
using Microsoft.EntityFrameworkCore;

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

    [Fact]
    public async Task CanDeserializeExampleSentenceTranslationSnapshotFromHarmony()
    {
        // arrange
        await using var apiFixture = MiniLcmApiFixture.Create(false);
        await apiFixture.InitializeAsync();
        var dbContext = apiFixture.DbContext;
        using var dbConnection = dbContext.Database.GetDbConnection();
        await dbConnection.OpenAsync();
        var dbCommand = dbConnection.CreateCommand();
        var snapshotId = Guid.Parse("11183194-90E8-45DE-839B-E53FBC9BCFB7");
        dbCommand.CommandText = $$$"""
INSERT INTO Commits VALUES('4449DA29-2187-4D2E-9C67-92C295C3FBD7','E412344CF0C2090F','56F7E637ED4D61B6',0,'2025-09-11 04:41:51.1824641','{"AuthorName":null,"AuthorId":null,"ClientVersion":"v2025-09-05-d8051b6c\u002Bd8051b6cc48f8b51b22b9231cd45d23ab59bdc00","ExtraMetadata":{}}','ED59326F-B0AA-406F-857A-50FA2B1DC417');
INSERT INTO Snapshots VALUES('{{{snapshotId.ToString().ToUpper()}}}','ExampleSentence','{"$type":"MiniLcmCrdtAdapter","Obj":{"$type":"ExampleSentence","Id":"222d7c84-95d8-4707-a400-e1f3619d90fb","Order":1,"Sentence":{"en":{"Spans":[{"Text":"We ate an apple","Ws":"en"}]}},"Translation":{"en":{"Spans":[{"Text":"translation","Ws":"en"}]}},"SenseId":"b9440091-a9fc-4769-82b1-2ee8d030808d","DeletedAt":null},"Id":"222d7c84-95d8-4707-a400-e1f3619d90fb","DeletedAt":null}','["33340091-A9FC-4769-82B1-2EE8D030808D"]','222D7C84-95D8-4707-A400-E1F3619D90FB',0,'4449DA29-2187-4D2E-9C67-92C295C3FBD7',1);
""";
        await dbCommand.ExecuteNonQueryAsync();

        // act
        var dataModel = apiFixture.DataModel;
        var adapter = await dataModel.GetBySnapshotId<MiniLcmCrdtAdapter>(snapshotId);

        // assert
        var exampleSentence = adapter.DbObject.Should().BeOfType<ExampleSentence>().Subject;
        exampleSentence.Translations.Should().ContainSingle().Subject.Text["en"].Should().BeEquivalentTo(new RichString("translation", "en"));
    }
}
