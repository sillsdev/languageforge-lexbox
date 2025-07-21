using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Tests.Fixtures;
using FwHeadless;
using FwHeadless.Media;
using LexCore.Entities;
using LexData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MiniLcm;
using MiniLcm.Media;
using SIL.LCModel;
using Testing.Fixtures;

namespace Testing.FwHeadless;

[Collection(nameof(TestingServicesFixture))]
[Trait("Category", "RequiresDb")]
public class MediaFileServiceTests : IDisposable
{
    private readonly MediaFileService _service;
    private readonly LexboxFwDataMediaAdapter _adapter;
    private readonly FwHeadlessConfig _fwHeadlessConfig;
    private readonly LcmCache _cache;
    private readonly LexBoxDbContext _lexBoxDbContext;
    private readonly Guid _projectId = SeedingData.Sena3ProjId;

    public MediaFileServiceTests(TestingServicesFixture testing)
    {
        var services = testing.ConfigureServices(s => s.AddTestFwDataBridge());
        _fwHeadlessConfig = new FwHeadlessConfig()
        {
            LexboxUrl = "test",
            LexboxUsername = "admin",
            LexboxPassword = "pass",
            ProjectStorageRoot = Path.GetFullPath(Path.Combine(".", $"SyncMediaFileTests-{Guid.NewGuid():N}")),
            MediaFileAuthority = "localhost"
        };
        Directory.CreateDirectory(_fwHeadlessConfig.ProjectStorageRoot);
        _cache = services.GetRequiredService<MockFwProjectLoader>()
            .NewProject(_fwHeadlessConfig.GetFwDataProject("sena-3", _projectId), "en", "en");
        Directory.CreateDirectory(_cache.LangProject.LinkedFilesRootDir);
        _lexBoxDbContext = services.GetDbContext();
        var config = new OptionsWrapper<FwHeadlessConfig>(_fwHeadlessConfig);
        _service = new MediaFileService(_lexBoxDbContext, config);
        _adapter = new LexboxFwDataMediaAdapter(config, _service);
    }

    public void Dispose()
    {
        Directory.Delete(_fwHeadlessConfig.ProjectStorageRoot, true);
        _lexBoxDbContext.Files.ExecuteDelete();
    }

    private string RelativeToLinkedFiles(string path)
    {
        if (Path.IsPathRooted(path)) return Path.GetRelativePath(_cache.LangProject.LinkedFilesRootDir, path);
        path.Should().StartWith("LinkedFiles");
        return Path.GetRelativePath("LinkedFiles", path);
    }

    private async Task<MediaFile> AddFile(string fileName)
    {
        AddFwFile(fileName);
        var file = await AddDbFile(fileName);
        return file;
    }

    private void AddFwFile(string fileName)
    {
        File.WriteAllText(Path.Join(_cache.LangProject.LinkedFilesRootDir, fileName), "test");
    }

    private async Task<MediaFile> AddDbFile(string fileName)
    {
        var mediaFile = new MediaFile()
        {
            Filename = Path.Join(
                Path.GetRelativePath(_cache.ProjectId.ProjectFolder, _cache.LangProject.LinkedFilesRootDir),
                fileName),
            ProjectId = _projectId
        };
        _lexBoxDbContext.Files.Add(mediaFile);
        await _lexBoxDbContext.SaveChangesAsync();
        return mediaFile;
    }

    private async Task AssertDbFileExists(string fileName)
    {
        var files = await _lexBoxDbContext.Files.Where(f => f.ProjectId == _projectId).ToArrayAsync();
        files.Should().Contain(f => f.Filename.EndsWith(fileName));
    }

    private async Task AssertDbFileDoesNotExist(string fileName)
    {
        var files = await _lexBoxDbContext.Files.Where(f => f.ProjectId == _projectId).ToArrayAsync();
        files.Should().NotContain(f => f.Filename.EndsWith(fileName));
    }

    [Fact]
    public async Task Sync_NothingWorks()
    {
        var result = await _service.SyncMediaFiles(_cache);
        result.Added.Should().BeEmpty();
        result.Removed.Should().BeEmpty();
    }

    [Fact]
    public async Task Sync_NewFwFilesGetAddedToDb()
    {
        AddFwFile("NewFile.txt");

        var result = await _service.SyncMediaFiles(_cache);
        result.Added.Should().HaveCount(1);
        result.Removed.Should().BeEmpty();

        await AssertDbFileExists("NewFile.txt");
    }

    [Fact]
    public async Task Sync_FilesMissingFromFwGetRemoved()
    {
        await AddDbFile("NewDbFile.txt");

        await AssertDbFileExists("NewDbFile.txt");

        var result = await _service.SyncMediaFiles(_cache);
        result.Added.Should().BeEmpty();
        result.Removed.Should().HaveCount(1);

        await AssertDbFileDoesNotExist("NewDbFile.txt");
    }

    [Fact]
    public async Task Sync_PreExistingFilesArePreserved()
    {
        AddFwFile("SomeFile.txt");
        await AddDbFile("SomeFile.txt");

        var result = await _service.SyncMediaFiles(_cache);
        result.Added.Should().BeEmpty();
        result.Removed.Should().BeEmpty();

        await AssertDbFileExists("SomeFile.txt");
    }

    [Fact]
    public async Task Adapter_ToMediaUri()
    {
        var mediaFile = await AddFile("Adapter_ToMediaUri.txt");
        var mediaUri = _adapter.MediaUriFromPath(RelativeToLinkedFiles(mediaFile.Filename), _cache);
        mediaUri.FileId.Should().Be(mediaFile.Id);
    }

    [Fact]
    public async Task Adapter_MediaUriToPath()
    {
        var mediaFile = await AddFile("Adapter_MediaUriToPath.txt");
        var path = _adapter.PathFromMediaUri(new MediaUri(mediaFile.Id, "test"), _cache);
        path.Should().Be("Adapter_MediaUriToPath.txt");
        Directory.EnumerateFiles(_cache.LangProject.LinkedFilesRootDir).Select(RelativeToLinkedFiles).Should().Contain(path);
    }
}
