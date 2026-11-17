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

    [Theory]
    // Distinct codes per type so a swapped analysis/vernacular argument would fail, and several additional
    // ones per type because CreateNewLangProj takes those as an unordered set
    [InlineData("en,de,pt,ru", "fr,es,it,nl", "en,de,pt,ru", "fr,es,it,nl")]
    // liblcm adds "en" as a current analysis WS whenever another one is the default
    [InlineData("de,pt", "fr", "de,pt", "fr")]
    // ...and appends a later "en" a second time
    [InlineData("de,en,pt", "fr", "de,en,pt", "fr")]
    // liblcm can't take a default analysis WS whose ICU locale differs from its tag ("pt_BR")
    [InlineData("pt-BR,en", "fr", "pt-BR,en", "fr")]
    [InlineData("qaa-x-foo", "fr", "qaa-x-foo", "fr")]
    // Tags liblcm treats as the same WS end up once
    [InlineData("en", "fr,fr-Latn", "en", "fr")]
    public async Task NewProjectHasExactlyTheRequestedWritingSystemsInOrder(
        string analysis, string vernacular, string expectedAnalysis, string expectedVernacular)
    {
        var fwDataBridgeConfig = Services.GetRequiredService<IOptions<FwDataBridgeConfig>>().Value;
        // The SIL.LCModel template ships next to the test assembly; point liblcm at it (this project,
        // unlike FwDataMiniLcmBridge.Tests, doesn't otherwise configure TemplatesFolder).
        fwDataBridgeConfig.TemplatesFolder = Path.Combine(AppContext.BaseDirectory, "Templates");
        var fwDataProject = new FwDataProject("multi-ws-source", fwDataBridgeConfig.ProjectsFolder);
        Services.GetRequiredService<IProjectLoader>()
            .NewProject(fwDataProject, analysis.Split(','), vernacular.Split(','), uiWs: "en").Dispose();

        // Reopened from disk, so this also checks the fixed-up lists were saved
        using var api = Services.GetRequiredService<FwDataFactory>().GetFwDataMiniLcmApi(fwDataProject, false);
        var writingSystems = await api.GetWritingSystems();
        writingSystems.Analysis.Select(ws => ws.WsId.Code).Should().Equal(expectedAnalysis.Split(','));
        writingSystems.Vernacular.Select(ws => ws.WsId.Code).Should().Equal(expectedVernacular.Split(','));
    }

    [Fact]
    public void ADefaultAnalysisWritingSystemLiblcmCantTakeStillGetsTheReversalIndex()
    {
        var fwDataBridgeConfig = Services.GetRequiredService<IOptions<FwDataBridgeConfig>>().Value;
        fwDataBridgeConfig.TemplatesFolder = Path.Combine(AppContext.BaseDirectory, "Templates");
        var fwDataProject = new FwDataProject("reversal-source", fwDataBridgeConfig.ProjectsFolder);

        // "en" is passed to liblcm as the default instead, so it would otherwise get the reversal index
        using var cache = Services.GetRequiredService<IProjectLoader>().NewProject(fwDataProject, ["pt-BR"], ["fr"], uiWs: "en");

        cache.LangProject.LexDbOA.ReversalIndexesOC.Select(ri => ri.WritingSystem).Should().Equal("pt-BR");
    }
}
