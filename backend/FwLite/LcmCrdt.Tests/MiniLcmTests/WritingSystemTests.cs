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
}
