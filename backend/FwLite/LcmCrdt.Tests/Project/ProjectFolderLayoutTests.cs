using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace LcmCrdt.Tests.Project;

public class ProjectFolderLayoutTests : IAsyncLifetime
{
    private readonly string _projectPath = Path.GetFullPath(Path.Join("project-folder-layout-tests", Guid.NewGuid().ToString("N")));
    private ServiceProvider? _services;

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_projectPath);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_services is not null) await _services.DisposeAsync();
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_projectPath)) Directory.Delete(_projectPath, true);
    }

    private CrdtProjectsService NewProjectsService(bool useProjectFolders)
    {
        _services = new ServiceCollection()
            .AddTestLcmCrdtClient()
            .Configure<LcmCrdtConfig>(config =>
            {
                config.ProjectPath = _projectPath;
                config.UseProjectFolders = useProjectFolders;
            })
            .BuildServiceProvider();
        return _services.GetRequiredService<CrdtProjectsService>();
    }

    [Fact]
    public async Task CreateProject_UsesProjectFolder()
    {
        var service = NewProjectsService(useProjectFolders: true);

        var project = await service.CreateProject(new("Folder Project", "folder-project"));

        project.DbPath.Should().Be(Path.Join(_projectPath, "projects", "folder-project", "folder-project.sqlite"));
        File.Exists(project.DbPath).Should().BeTrue();
        service.GetProject("folder-project")!.DbPath.Should().Be(project.DbPath);
    }

    [Fact]
    public async Task CreateProject_StaysFlatWhenProjectFoldersAreOff()
    {
        var service = NewProjectsService(useProjectFolders: false);

        var project = await service.CreateProject(new("Flat Project", "flat-project"));

        project.DbPath.Should().Be(Path.Join(_projectPath, "flat-project.sqlite"));
        Directory.Exists(Path.Join(_projectPath, "projects")).Should().BeFalse();
    }

    [Fact]
    public async Task ListProjects_FindsBothLayouts()
    {
        var flat = await NewProjectsService(useProjectFolders: false).CreateProject(new("Flat", "flat"));
        await _services!.DisposeAsync();
        var service = NewProjectsService(useProjectFolders: true);
        var nested = await service.CreateProject(new("Nested", "nested"));

        service.ListProjects().Select(p => (p.Name, p.DbPath)).Should().BeEquivalentTo([
            ("flat", flat.DbPath),
            ("nested", nested.DbPath)
        ]);
    }

    [Fact]
    public async Task ProjectFolderWinsOverFlatFileWithTheSameCode()
    {
        var service = NewProjectsService(useProjectFolders: true);
        var nested = await service.CreateProject(new("Dupe", "dupe"));
        // not something the app creates; e.g. a user copied an old file back in
        File.Copy(nested.DbPath, Path.Join(_projectPath, "dupe.sqlite"));

        service.ListProjects().Should().ContainSingle().Which.DbPath.Should().Be(nested.DbPath);
        service.GetProject("dupe")!.DbPath.Should().Be(nested.DbPath);
    }

    [Fact]
    public async Task CreateProject_RejectsCodeThatExistsInTheFlatLayout()
    {
        await NewProjectsService(useProjectFolders: false).CreateProject(new("Existing", "existing"));
        await _services!.DisposeAsync();
        var service = NewProjectsService(useProjectFolders: true);

        var act = () => service.CreateProject(new("Existing", "existing"));

        await act.Should().ThrowAsync<InvalidOperationException>();
        Directory.Exists(Path.Join(_projectPath, "projects", "existing")).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteProject_RemovesTheProjectFolder()
    {
        var service = NewProjectsService(useProjectFolders: true);
        var project = await service.CreateProject(new("To Delete", "to-delete"));

        await service.DeleteProject("to-delete");

        Directory.Exists(Path.GetDirectoryName(project.DbPath)).Should().BeFalse();
        service.GetProject("to-delete").Should().BeNull();
    }

    [Fact]
    public async Task DeleteProject_LeavesOtherFilesInTheProjectFolder()
    {
        var service = NewProjectsService(useProjectFolders: true);
        var project = await service.CreateProject(new("Keep Folder", "keep-folder"));
        var otherFile = Path.Join(Path.GetDirectoryName(project.DbPath), "notes.txt");
        await File.WriteAllTextAsync(otherFile, "not ours");

        await service.DeleteProject("keep-folder");

        File.Exists(project.DbPath).Should().BeFalse();
        File.Exists(otherFile).Should().BeTrue();
    }
}
