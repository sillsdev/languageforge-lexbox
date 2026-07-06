using FwDataMiniLcmBridge.Api;
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

    private static WritingSystem NewVernacularWs(string code, bool isDisabled = false)
    {
        return new WritingSystem
        {
            Id = Guid.NewGuid(),
            Type = WritingSystemType.Vernacular,
            WsId = code,
            Name = code,
            Abbreviation = code,
            Font = "Arial",
            IsDisabled = isDisabled,
        };
    }

    [Fact]
    public async Task CreateDisabledWritingSystem_IsInFullListButNotCurrentList()
    {
        await Api.CreateWritingSystem(NewVernacularWs("es", isDisabled: true));

        var container = ((FwDataMiniLcmApi)BaseApi).Cache.ServiceLocator.WritingSystems;
        container.VernacularWritingSystems.Should().Contain(ws => ws.Id == "es");
        container.CurrentVernacularWritingSystems.Should().NotContain(ws => ws.Id == "es");
    }

    [Fact]
    public async Task DisablingWritingSystem_TogglesCurrentListMembership()
    {
        var es = await Api.CreateWritingSystem(NewVernacularWs("es"));
        var container = ((FwDataMiniLcmApi)BaseApi).Cache.ServiceLocator.WritingSystems;
        container.CurrentVernacularWritingSystems.Should().Contain(ws => ws.Id == "es");

        var disabled = es.Copy();
        disabled.IsDisabled = true;
        await Api.UpdateWritingSystem(es, disabled);
        container.VernacularWritingSystems.Should().Contain(ws => ws.Id == "es");
        container.CurrentVernacularWritingSystems.Should().NotContain(ws => ws.Id == "es");

        var reenabled = disabled.Copy();
        reenabled.IsDisabled = false;
        await Api.UpdateWritingSystem(disabled, reenabled);
        container.CurrentVernacularWritingSystems.Should().Contain(ws => ws.Id == "es");
    }

    [Fact]
    public async Task MoveDisabledWritingSystem_DoesNotEnableIt()
    {
        await Api.CreateWritingSystem(NewVernacularWs("es", isDisabled: true));
        await Api.CreateWritingSystem(NewVernacularWs("fr"));

        // move the disabled es to the front of the full list
        await Api.MoveWritingSystem("es", WritingSystemType.Vernacular, new(null, "en"));

        var container = ((FwDataMiniLcmApi)BaseApi).Cache.ServiceLocator.WritingSystems;
        container.CurrentVernacularWritingSystems.Should().NotContain(ws => ws.Id == "es");
        // enabled writing systems are still reported first, so the report order is unchanged
        var vernacular = (await Api.GetWritingSystems()).Vernacular;
        vernacular.Select(ws => ws.WsId.Code).Should().Equal("en", "fr", "es");
        (await Api.GetWritingSystem(default, WritingSystemType.Vernacular))!.WsId.Code.Should().Be("en");
    }

    [Fact]
    public async Task MoveWritingSystem_WithDisabledAnchor_DoesNotThrow()
    {
        await Api.CreateWritingSystem(NewVernacularWs("es", isDisabled: true));
        await Api.CreateWritingSystem(NewVernacularWs("fr"));

        // both anchors resolve to positions outside the current list (es is disabled)
        await Api.MoveWritingSystem("fr", WritingSystemType.Vernacular, new("es", null));

        var container = ((FwDataMiniLcmApi)BaseApi).Cache.ServiceLocator.WritingSystems;
        container.CurrentVernacularWritingSystems.Should().Contain(ws => ws.Id == "fr");
        container.CurrentVernacularWritingSystems.Should().NotContain(ws => ws.Id == "es");
        var vernacular = (await Api.GetWritingSystems()).Vernacular;
        vernacular.Select(ws => ws.WsId.Code).Should().Equal("en", "fr", "es");
    }
}
