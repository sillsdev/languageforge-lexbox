using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.LcmUtils;
using FwLiteProjectSync.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FwLiteProjectSync.Tests;

/// <summary>
/// ProjectLoader.NewProject with several writing systems of each type, against the real SIL.LCModel template.
/// </summary>
public class NewProjectWritingSystemsTests : IAsyncLifetime
{
    private const string ProjectFolder = nameof(NewProjectWritingSystemsTests);
    private readonly ServiceProvider _rootServiceProvider;
    private readonly AsyncServiceScope _scope;
    private IServiceProvider Services => _scope.ServiceProvider;

    public NewProjectWritingSystemsTests()
    {
        _rootServiceProvider = new ServiceCollection()
            .AddSyncServices(ProjectFolder, mockFwProjectLoader: false)
            .BuildServiceProvider();
        _scope = _rootServiceProvider.CreateAsyncScope();
    }

    public Task InitializeAsync()
    {
        if (Directory.Exists(ProjectFolder)) Directory.Delete(ProjectFolder, true);
        Directory.CreateDirectory(Services.GetRequiredService<IOptions<FwDataBridgeConfig>>().Value.ProjectsFolder);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _scope.DisposeAsync();
        await _rootServiceProvider.DisposeAsync();
    }

    [Fact]
    public async Task NewProjectAddsEveryRequestedWritingSystemToTheCorrectListInOrder()
    {
        var fwDataBridgeConfig = Services.GetRequiredService<IOptions<FwDataBridgeConfig>>().Value;
        // The SIL.LCModel template ships next to the test assembly; point liblcm at it (this project,
        // unlike FwDataMiniLcmBridge.Tests, doesn't otherwise configure TemplatesFolder).
        fwDataBridgeConfig.TemplatesFolder = Path.Combine(AppContext.BaseDirectory, "Templates");
        var fwDataProject = new FwDataProject("multi-ws-source", fwDataBridgeConfig.ProjectsFolder);
        // First of each list is the default; the rest go through CreateNewLangProj's additional-WS sets.
        // Distinct codes per type so a swapped analysis/vernacular argument would fail this test, and
        // several additional ones per type because CreateNewLangProj takes those as an unordered set.
        string[] analysis = ["en", "de", "pt", "ru"];
        string[] vernacular = ["fr", "es", "it", "nl"];
        Services.GetRequiredService<IProjectLoader>().NewProject(fwDataProject, analysis, vernacular, uiWs: "en").Dispose();

        using var api = Services.GetRequiredService<FwDataFactory>().GetFwDataMiniLcmApi(fwDataProject, false);
        var writingSystems = await api.GetWritingSystems();
        writingSystems.Analysis.Select(ws => ws.WsId.Code).Should().Equal(analysis);
        writingSystems.Vernacular.Select(ws => ws.WsId.Code).Should().Equal(vernacular);
    }
}
