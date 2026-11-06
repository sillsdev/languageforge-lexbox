using System.Text.Json;
using SIL.Harmony.Changes;

namespace LcmCrdt.Tests.Changes;

// jsonPatch:ExampleSentence changes with a /Translation/ path were authored before example sentences had multiple translations.
// Applying one creates the first translation, so its id must come out the same every time the change is replayed.
public class LegacyTranslationPatchReplayTests(MiniLcmApiFixture fixture) : IClassFixture<MiniLcmApiFixture>
{
    private static readonly Guid ClientId = Guid.NewGuid();

    [Fact]
    public async Task TranslationEdit_SurvivesSnapshotRegeneration()
    {
        var entry = await fixture.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "apple" } });
        var sense = await fixture.Api.CreateSense(entry.Id, new Sense());
        var example = await fixture.Api.CreateExampleSentence(entry.Id, sense.Id,
            new ExampleSentence { Sentence = { ["en"] = new RichString("We ate an apple", "en") } });
        var legacyPatch = JsonSerializer.Deserialize<IChange>($$"""
            {
              "$type": "jsonPatch:ExampleSentence",
              "PatchDocument": [
                { "op": "add", "path": "/Translation/en", "value": { "Spans": [{ "Text": "legacy", "Ws": "en" }] } }
              ],
              "EntityId": "{{example.Id}}"
            }
            """, TestJsonOptions.Harmony());
        await fixture.DataModel.AddChange(ClientId, legacyPatch!);

        var translationId = (await fixture.Api.GetExampleSentence(entry.Id, sense.Id, example.Id))!
            .Translations.Should().ContainSingle().Subject.Id;
        await fixture.Api.UpdateTranslation(entry.Id, sense.Id, example.Id, translationId,
            new UpdateObjectInput<Translation>().Set(t => t.Text["en"], new RichString("edited", "en")));

        await fixture.DataModel.RegenerateSnapshots();

        var translation = (await fixture.Api.GetExampleSentence(entry.Id, sense.Id, example.Id))!
            .Translations.Should().ContainSingle().Subject;
        translation.Id.Should().Be(translationId);
        translation.Text["en"].Should().BeEquivalentTo(new RichString("edited", "en"));
    }
}
