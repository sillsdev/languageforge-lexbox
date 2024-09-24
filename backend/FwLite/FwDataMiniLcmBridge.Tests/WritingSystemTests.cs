using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm;

namespace FwDataMiniLcmBridge.Tests;

[Collection(ProjectLoaderFixture.Name)]
public class WritingSystemTests(ProjectLoaderFixture fixture) : IAsyncLifetime
{
    private FwDataMiniLcmApi _api = null!;

    public async Task InitializeAsync()
    {
        var projectName = "ws-test_" + Guid.NewGuid();
        fixture.MockFwProjectLoader.NewProject(projectName, "en", "en");
        _api = fixture.CreateApi(projectName);
        _api.Should().NotBeNull();


        await _api.CreateEntry(new Entry()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "new-lexeme-form" } },
            Senses = new List<Sense>() { new Sense() { Gloss = { { "en", "new-sense-gloss" } } } }
        });
    }

    public Task DisposeAsync()
    {
        _api.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetWritingSystems_DoesNotReturnNullOrEmpty()
    {
        var writingSystems = await _api.GetWritingSystems();
        writingSystems.Vernacular.Should().NotBeNullOrEmpty();
        writingSystems.Analysis.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetWritingSystems_ReturnsExemplars()
    {
        var writingSystems = await _api.GetWritingSystems();
        writingSystems.Vernacular.Should().Contain(ws => ws.Exemplars.Any());
    }
}
