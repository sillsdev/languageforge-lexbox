using MiniLcm;
using MiniLcm.Models;
using MiniLcm.Tests;

namespace LcmCrdt.Tests.MiniLcmTests;

public class WritingSystemTests : WritingSystemTestsBase
{
    private readonly MiniLcmApiFixture _fixture = new();

    protected override async Task<IMiniLcmApi> NewApi()
    {
        await _fixture.InitializeAsync();
        var api = _fixture.Api;
        return api;
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task CreateWritingSystem_UsesTheIdPassedInId()
    {
        var id = Guid.NewGuid();
        await _fixture.Api.CreateWritingSystem(new()
        {
            Id = id,
            Type = WritingSystemType.Vernacular,
            WsId = "es",
            Name = "Spanish",
            Abbreviation = "Es",
            Font = "Arial"
        });
        var createdWs = await _fixture.Api.GetWritingSystem("es", WritingSystemType.Vernacular);
        createdWs.Should().NotBeNull();
        createdWs.Id.Should().Be(id);
    }

    [Fact]
    public async Task CreateWritingSystem_HonorsFont()
    {
        var ws = await _fixture.Api.CreateWritingSystem(new()
        {
            Id = Guid.NewGuid(),
            Type = WritingSystemType.Vernacular,
            WsId = "es",
            Name = "Spanish",
            Abbreviation = "Es",
            Font = "Arial"
        });
        ws.Font.Should().Be("Arial");
    }

    private async Task CreateWsWithRules()
    {
        await Api.CreateWritingSystem(new()
        {
            Id = Guid.NewGuid(),
            Type = WritingSystemType.Vernacular,
            WsId = "es",
            Name = "Spanish",
            Abbreviation = "Es",
            Font = "Arial",
            IcuCollationRules = "&z < a",
        });
    }

    [Fact]
    public async Task UpdateWritingSystem_PatchSettingLocaleWithoutClearingRules_Throws()
    {
        await CreateWsWithRules();

        var act = () => Api.UpdateWritingSystem("es", WritingSystemType.Vernacular,
            new UpdateObjectInput<WritingSystem>().Set(ws => ws.SystemCollationLocale, "sv"));

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
        var ws = await Api.GetWritingSystem("es", WritingSystemType.Vernacular);
        ws!.SystemCollationLocale.Should().BeNull();
    }

    [Fact]
    public async Task UpdateWritingSystem_PatchSwitchingRulesToLocale_Works()
    {
        await CreateWsWithRules();

        await Api.UpdateWritingSystem("es", WritingSystemType.Vernacular,
            new UpdateObjectInput<WritingSystem>()
                .Set(ws => ws.IcuCollationRules, null)
                .Set(ws => ws.SystemCollationLocale, "sv"));

        var ws = await Api.GetWritingSystem("es", WritingSystemType.Vernacular);
        ws!.IcuCollationRules.Should().BeNull();
        ws.SystemCollationLocale.Should().Be("sv");
    }
}
