using Microsoft.Data.Sqlite;
using FwLiteShared.LocalSync;
using LcmCrdt.MediaServer;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm.Media;
using static LcmCrdt.Tests.LocalSync.LocalSyncTestHelpers;

namespace LcmCrdt.Tests.LocalSync;

public class OpenFromFolderTests : IAsyncLifetime
{
    private static readonly Guid ProjectId = Guid.Parse("2f4a6c8e-0b1d-4e3f-a5c7-9e1b3d5f7a68");
    private readonly TempFolder _sourceFiles = new();
    private readonly MiniLcmApiFixture _source;
    private readonly TempFolder _folder = new();
    private readonly TempFolder _otherDeviceProjects = new();
    private readonly MiniLcmApiFixture _otherDevice;

    public OpenFromFolderTests()
    {
        _source = MiniLcmApiFixture.Create(projectId: ProjectId, configureServices: services =>
        {
            AddLocalSyncServices(services);
            services.Configure<LcmCrdtConfig>(config => config.ProjectPath = _sourceFiles.Path);
        });
        // a second device, whose projects live in their own directory
        _otherDevice = MiniLcmApiFixture.Create(projectId: Guid.NewGuid(), configureServices: services =>
        {
            AddLocalSyncServices(services);
            services.Configure<LcmCrdtConfig>(config => config.ProjectPath = _otherDeviceProjects.Path);
        });
    }

    public async Task InitializeAsync()
    {
        await _source.InitializeAsync();
        await _otherDevice.InitializeAsync();
        await MakeEditor(_source, "source-user");
        await _source.GetService<CurrentProjectService>().SetProjectSyncOrigin(new Uri("https://lexbox.org"), null);
    }

    public async Task DisposeAsync()
    {
        await _otherDevice.DisposeAsync();
        await _source.DisposeAsync();
        _folder.Dispose();
        // pooled connections keep the downloaded projects' files open; release just those (ClearAllPools breaks parallel tests)
        foreach (var file in Directory.EnumerateFiles(_otherDeviceProjects.Path, "*.sqlite"))
        {
            using var connection = new SqliteConnection($"Data Source={file}");
            SqliteConnection.ClearPool(connection);
        }
        _otherDeviceProjects.Dispose();
        _sourceFiles.Dispose();
    }

    private async Task<OpenFromFolderResult> OpenOnOtherDevice() =>
        await _otherDevice.GetService<FolderProjectOpener>().OpenFromFolder(_folder.Path);

    private async Task<Entry?> GetEntryOnOtherDevice(string projectCode, Guid entryId)
    {
        var project = _otherDevice.GetService<CrdtProjectsService>().GetProject(projectCode)!;
        await using var scope = _otherDevice.GetService<IServiceProvider>().CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CurrentProjectService>().SetupProjectContext(project);
        return await scope.ServiceProvider.GetRequiredService<IMiniLcmApi>().GetEntry(entryId);
    }

    [Fact]
    public async Task AProjectThisDeviceDoesNotHave_IsDownloadedFromTheFolder()
    {
        var apple = await _source.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "apple" } });
        await _source.GetService<FolderSyncService>().SyncWithFolder(_folder.Path);

        var result = await OpenOnOtherDevice();

        result.Outcome.Should().Be(OpenFromFolderOutcome.Downloaded);
        result.ProjectCode.Should().Be("sena-3");
        var project = _otherDevice.GetService<CrdtProjectsService>().GetProject("sena-3")!;
        project.Data!.Id.Should().Be(ProjectId);
        project.Data.OriginDomain.Should().Be("https://lexbox.org", "so the copy can later sync with Lexbox");
        (await GetEntryOnOtherDevice("sena-3", apple.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task PicturesComeWithAProjectOpenedFromTheFolder()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("pretend this is a jpeg");
        var (picture, _) = await _source.GetService<LcmMediaService>()
            .SaveFile(new MemoryStream(bytes), new LcmFileMetadata("apple.jpg", "image/jpeg"));
        await _source.GetService<FolderSyncService>().SyncWithFolder(_folder.Path);

        await OpenOnOtherDevice();

        var project = _otherDevice.GetService<CrdtProjectsService>().GetProject("sena-3")!;
        await using var scope = _otherDevice.GetService<IServiceProvider>().CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CurrentProjectService>().SetupProjectContext(project);
        await using var file = await scope.ServiceProvider.GetRequiredService<LcmMediaService>()
            .GetFileStream(picture.Id, downloadIfMissing: false);
        file.Result.Should().Be(ReadFileResult.Success);
    }

    [Fact]
    public async Task AProjectThisDeviceAlreadyHas_IsSyncedWithTheFolder()
    {
        await _source.GetService<FolderSyncService>().SyncWithFolder(_folder.Path);
        (await OpenOnOtherDevice()).Outcome.Should().Be(OpenFromFolderOutcome.Downloaded);
        var banana = await _source.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "banana" } });
        await _source.GetService<FolderSyncService>().SyncWithFolder(_folder.Path);

        var result = await OpenOnOtherDevice();

        result.Outcome.Should().Be(OpenFromFolderOutcome.Synced);
        (await GetEntryOnOtherDevice("sena-3", banana.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task AProjectWhoseCodeIsTakenByADifferentLocalProject_IsNotDownloaded()
    {
        await _source.GetService<FolderSyncService>().SyncWithFolder(_folder.Path);
        await _otherDevice.GetService<CrdtProjectsService>().CreateProject(new("Another Sena", "sena-3"));

        var result = await OpenOnOtherDevice();

        result.Outcome.Should().Be(OpenFromFolderOutcome.ProjectCodeInUse);
    }

    [Fact]
    public async Task AFolderWithNoProject_OpensNothing()
    {
        var result = await OpenOnOtherDevice();

        result.Outcome.Should().Be(OpenFromFolderOutcome.EmptyFolder);
    }
}
