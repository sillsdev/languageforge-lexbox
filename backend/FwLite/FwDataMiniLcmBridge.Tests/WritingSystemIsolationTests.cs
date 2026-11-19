using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.LcmUtils;
using FwDataMiniLcmBridge.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MiniLcm.Models;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Utils;

namespace FwDataMiniLcmBridge.Tests;

public class WritingSystemIsolationTests : IDisposable
{
    private readonly ServiceProvider _services = new ServiceCollection()
        .AddTestFwDataBridge(mockProjectLoader: false)
        .BuildServiceProvider();
    private readonly List<FwDataProject> _projects = [];
    private readonly string _wsTag = $"qaa-x-{Guid.NewGuid().ToString("N")[..8]}";

    public void Dispose()
    {
        _services.Dispose();
        SingletonsContainer.Get<CoreGlobalWritingSystemRepository>().Remove(_wsTag);
        foreach (var project in _projects)
        {
            if (Directory.Exists(project.ProjectFolder)) Directory.Delete(project.ProjectFolder, true);
        }
    }

    // FwHeadless opens many unrelated projects in one process; they must not share writing system settings.
    [Fact]
    public async Task NewWritingSystem_DoesNotInheritSettingsFromAnotherProject()
    {
        var factory = _services.GetRequiredService<FwDataFactory>();

        var projectA = NewProject();
        var apiA = factory.GetFwDataMiniLcmApi(projectA, false);
        await apiA.CreateWritingSystem(NewWritingSystem());
        await apiA.UpdateWritingSystem(_wsTag, WritingSystemType.Vernacular,
            new UpdateObjectInput<WritingSystem>().Set(ws => ws.Font, "Project A Font"));
        await factory.CloseProjectAsync(projectA);

        var apiB = factory.GetFwDataMiniLcmApi(NewProject(), false);
        var wsB = await apiB.CreateWritingSystem(NewWritingSystem());

        wsB.Font.Should().NotBe("Project A Font");
    }

    private FwDataProject NewProject()
    {
        var config = _services.GetRequiredService<IOptions<FwDataBridgeConfig>>().Value;
        Directory.CreateDirectory(config.ProjectsFolder);
        var project = new FwDataProject($"ws-isolation-test_{Guid.NewGuid()}", config.ProjectsFolder);
        _services.GetRequiredService<IProjectLoader>().NewProject(project, "en", "en");
        _projects.Add(project);
        return project;
    }

    private WritingSystem NewWritingSystem() => new()
    {
        Id = Guid.NewGuid(),
        Type = WritingSystemType.Vernacular,
        WsId = _wsTag,
        Name = "Test",
        Abbreviation = "Test",
        Font = "Arial"
    };
}
