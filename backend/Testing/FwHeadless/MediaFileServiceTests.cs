using Chorus.VcsDrivers.Mercurial;
using FwDataMiniLcmBridge.Tests.Fixtures;
using FwHeadless;
using FwHeadless.Media;
using FwHeadless.Services;
using LexData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MiniLcm.Media;
using SIL.LCModel;
using SIL.Progress;
using Testing.Fixtures;
using MediaFile = LexCore.Entities.MediaFile;

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
    private readonly HgRepository _hgRepository;

    public MediaFileServiceTests(TestingServicesFixture testing)
    {
        var services = testing.ConfigureServices(s => s.AddFwHeadless().AddTestFwDataBridge().Configure<FwHeadlessConfig>(config =>
        {
            config.LexboxUrl = "http://localhost/";
            config.LexboxUsername = "admin";
            config.LexboxPassword = "pass";
            config.ProjectStorageRoot = Path.GetFullPath(Path.Combine(".", $"SyncMediaFileTests-{Guid.NewGuid():N}"));
            config.MediaFileAuthority = "localhost";
        }));
        _fwHeadlessConfig = services.GetRequiredService<IOptions<FwHeadlessConfig>>().Value;
        Directory.CreateDirectory(_fwHeadlessConfig.ProjectStorageRoot);
        var fwDataProject = _fwHeadlessConfig.GetFwDataProject("sena-3", _projectId);
        _cache = services.GetRequiredService<MockFwProjectLoader>()
            .NewProject(fwDataProject, "en", "en");
        Directory.CreateDirectory(_cache.LangProject.LinkedFilesRootDir);
        _hgRepository = HgRepository.CreateOrUseExisting(fwDataProject.ProjectFolder, new NullProgress());
        _lexBoxDbContext = services.GetDbContext();
        var config = new OptionsWrapper<FwHeadlessConfig>(_fwHeadlessConfig);
        _service = new MediaFileService(_lexBoxDbContext, config, services.GetRequiredService<ISendReceiveService>());
        _adapter = new LexboxFwDataMediaAdapter(config, _service);
    }

    public void Dispose()
    {
        Directory.Delete(_fwHeadlessConfig.ProjectStorageRoot, true);
        _lexBoxDbContext.Files.ExecuteDelete();
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

    private string FullFilePath(MediaFile mediaFile)
    {
        return Path.Join(_cache.ProjectId.ProjectFolder, mediaFile.Filename);
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
    public async Task SaveMediaFile_Works()
    {
        var mediaFile = new MediaFile
        {
            Filename = "test.txt",
            ProjectId = _projectId
        };
        await _service.SaveMediaFile(mediaFile, SimpleStream("test"));

        await AssertDbFileExists("test.txt");
        var filePath = _service.FilePath(mediaFile);
        Directory.GetFiles(Path.GetDirectoryName(filePath)!).Should().Contain(filePath);
        (await File.ReadAllTextAsync(filePath)).Should().Be("test");
    }

    private Stream SimpleStream(string content)
    {
        var memoryStream = new MemoryStream();
        using (var stream = new StreamWriter(memoryStream, leaveOpen: true))
        {
            stream.Write(content);
        }

        memoryStream.Position = 0;
        return memoryStream;
    }

    [Fact]
    public async Task SaveMediaFile_HgRollbackDoesNotDeleteFile()
    {
        var mediaFile = new MediaFile
        {
            Filename = "test.txt",
            ProjectId = _projectId
        };
        await _service.SaveMediaFile(mediaFile, SimpleStream("test"));
        var currentRev = CurrentRev();
        currentRev.Should().Be(0, "Rev 0 is the commit of the test file");

        var filePath = _service.FilePath(mediaFile);
        var directoryName = Path.GetDirectoryName(filePath)!;
        Directory.GetFiles(directoryName).Should().Contain(filePath);

        //make a commit which would be rolled back by S&R during a conflict
        var conflictFile = Path.Join(_cache.LangProject.LinkedFilesRootDir, "conflict.txt");
        await File.WriteAllTextAsync(conflictFile, "test");
        await SendReceiveHelpers.CommitFile(conflictFile, "test commit");

        currentRev = CurrentRev();
        currentRev.Should().Be(1, "Rev 0 is the commit of the test file, rev 1 is the commit of the conflict file, current log: " + _hgRepository.GetLog(0));

        //simulate rollback as seen here: https://github.com/sillsdev/chorus/blob/af6e5c0e97758aef00bd2104b6c1ccf5719798ef/src/LibChorus/sync/Synchronizer.cs#L574
        _hgRepository.RollbackWorkingDirectoryToRevision((currentRev - 1).ToString());
        if (File.Exists(conflictFile))
            Directory.GetFiles(Path.GetDirectoryName(conflictFile)!).Should().NotContain(conflictFile);//deleted by rollback

        //file should not be deleted
        Directory.GetFiles(Path.GetDirectoryName(filePath)!).Should().Contain(filePath);
    }

    private int CurrentRev()
    {
        return int.Parse(_hgRepository.GetHeads().Single().Number.LocalRevisionNumber);
    }


    [Fact]
    public async Task SaveMediaFile_CanUpdateExistingFile()
    {
        var mediaFile = new MediaFile { Filename = "test.txt", ProjectId = _projectId };
        await _service.SaveMediaFile(mediaFile, SimpleStream("test"));
        _lexBoxDbContext.ChangeTracker.Clear();
        var fileId = mediaFile.Id;
        var actualFile = await _service.FindMediaFileAsync(fileId);
        actualFile.Should().NotBeNull();
        actualFile.Filename.Should().Be("test.txt");

        await _service.SaveMediaFile(actualFile, SimpleStream("updated"));

        (await File.ReadAllTextAsync(_service.FilePath(actualFile))).Should().Be("updated");
    }

    [Fact]
    public async Task SaveMediaFile_ThrowsWhenTheFileIsTooBig()
    {
        var memoryStream = new MemoryStream();
        using (var stream = new StreamWriter(memoryStream, leaveOpen: true))
        {
            while (memoryStream.Length < _fwHeadlessConfig.MaxUploadFileSizeBytes)
            {
                await stream.WriteAsync(Guid.NewGuid().ToString("N"));
            }
        }
        memoryStream.Position = 0;


        var mediaFile = new MediaFile { Filename = "test.txt", ProjectId = _projectId };
        var act = async () => await _service.SaveMediaFile(mediaFile, memoryStream);
        await act.Should().ThrowAsync<FileTooLarge>();

        var filePath = _service.FilePath(mediaFile);
        Directory.GetFiles(Path.GetDirectoryName(filePath)!).Should().NotContain(filePath);
    }

    [Fact]
    public async Task Adapter_ToMediaUri()
    {
        var mediaFile = await AddFile("Adapter_ToMediaUri.txt");
        var mediaUri = _adapter.MediaUriFromPath(FullFilePath(mediaFile), _cache);
        mediaUri.FileId.Should().Be(mediaFile.Id);
    }

    [Fact]
    public async Task Adapter_MediaUriToPath()
    {
        var mediaFile = await AddFile("Adapter_MediaUriToPath.txt");
        var path = _adapter.PathFromMediaUri(new MediaUri(mediaFile.Id, "test"), _cache);
        path.Should().Be(FullFilePath(mediaFile));
        Directory.EnumerateFiles(_cache.LangProject.LinkedFilesRootDir).Should().Contain(path);
    }
}
