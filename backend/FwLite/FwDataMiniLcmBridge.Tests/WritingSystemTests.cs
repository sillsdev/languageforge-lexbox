using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm;
using MiniLcm.Models;
using MiniLcm.Tests;

namespace FwDataMiniLcmBridge.Tests;

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
}
