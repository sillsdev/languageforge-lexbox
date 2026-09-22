using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm.Models;

namespace FwDataMiniLcmBridge.Tests.MiniLcmTests;

[Collection(ProjectLoaderFixture.Name)]
public class WritingSystemTests(ProjectLoaderFixture fixture) : WritingSystemTestsBase
{
    protected override async Task<IMiniLcmApi> NewApi()
    {
        var api = fixture.NewProjectApi("ws-test", "en", "en");

        //exemplars come from entries so we need to create one
        await api.CreateEntry(new Entry()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "new-lexeme-form" } },
            Senses = new List<Sense>() { new Sense() { Gloss = { { "en", "new-sense-gloss" } } } }
        });
        return api;
    }

    [Fact]
    public async Task CreateWritingSystem_IgnoresFont()
    {
        var ws = await Api.CreateWritingSystem(new()
        {
            Id = Guid.NewGuid(),
            Type = WritingSystemType.Vernacular,
            WsId = "es",
            Name = "Spanish",
            Abbreviation = "Es",
            Font = "Arial"
        });
        // the input Font is intentionally dropped (see FwDataMiniLcmApi.CreateWritingSystem);
        // "Charis SIL" is liblcm's default for a fresh writing system
        ws.Font.Should().Be("Charis SIL");
    }
}
