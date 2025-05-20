using System.Text.Json;
using Xunit.Abstractions;

namespace MiniLcm.Tests;

public class SerializationTests(ITestOutputHelper output)
{
    [Fact]
    public void CanSerializeEntry()
    {
        var entryId = Guid.NewGuid();
        var entry = new Entry()
        {
            Id = entryId,
            LexemeForm = { { "en", "test" } },
            CitationForm = { { "en", "test" } },
            Senses =
            [
                new Sense
                {
                    Id = Guid.NewGuid(),
                    EntryId = entryId,
                    Gloss = { { "en", "test" } }
                }
            ]
        };
        var act = () => JsonSerializer.Serialize(entry);
        var json = act.Should().NotThrow().Subject;
        output.WriteLine(json);
    }

    [Fact]
    public void CanDeserializeEntry()
    {
        var entryId = Guid.NewGuid();
        var senseId = Guid.NewGuid();
        var entry = new Entry()
        {
            Id = entryId,
            LexemeForm = { { "en", "test" } },
            CitationForm = { { "en", "test" } },
            Senses =
            [
                new Sense
                {
                    Id = senseId,
                    EntryId = entryId,
                    Gloss = { { "en", "test" } },
                    ExampleSentences =
                    {
                        new ExampleSentence()
                        {
                            Id = Guid.NewGuid(),
                            SenseId = senseId,
                            Sentence = { { "en", new RichString("this is only a test") } }
                        }
                    }
                }
            ]
        };
        var json = JsonSerializer.Serialize(entry);
        var act = () => JsonSerializer.Deserialize<Entry>(json);
        var fromJson = act.Should().NotThrow().Subject;
        fromJson.Should().BeEquivalentTo(entry);
    }


    [Fact]
    public void EqualityTest()
    {
        var entryId = Guid.NewGuid();
        var senseId = Guid.NewGuid();
        var entry = new Entry()
        {
            Id = entryId,
            LexemeForm = { { "en", "test" } },
            CitationForm = { { "en", "test" } },
            Senses =
            [
                new Sense
                {
                    Id = senseId,
                    EntryId = entryId,
                    Gloss = { { "en", "test" } }
                }
            ]
        };
        var entryCopy = new Entry()
        {
            Id = entryId,
            LexemeForm = { { "en", "test" } },
            CitationForm = { { "en", "test" } },
            Senses =
            [
                new Sense
                {
                    Id = senseId,
                    EntryId = entryId,
                    Gloss = { { "en", "test" } }
                }
            ]
        };
        entry.Should().BeEquivalentTo(entryCopy);
    }
}
