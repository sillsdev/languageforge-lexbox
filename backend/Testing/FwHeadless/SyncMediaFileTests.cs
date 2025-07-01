using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Tests.Fixtures;
using FwHeadless;
using FwHeadless.Media;
using LexCore.Entities;
using LexData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SIL.LCModel;
using Testing.Fixtures;

namespace Testing.FwHeadless;

[Collection(nameof(TestingServicesFixture))]
[Trait("Category", "RequiresDb")]
public class SyncMediaFileTests : IDisposable
{
    private readonly MediaFileService _service;
    private readonly FwHeadlessConfig _fwHeadlessConfig;
    private readonly LcmCache _cache;
    private readonly LexBoxDbContext _lexBoxDbContext;
    private readonly Guid _projectId = SeedingData.Sena3ProjId;

    public SyncMediaFileTests(TestingServicesFixture testing)
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
        _service = new MediaFileService(_lexBoxDbContext,
            new OptionsWrapper<FwHeadlessConfig>(_fwHeadlessConfig));
    }

    public void Dispose()
    {
        Directory.Delete(_fwHeadlessConfig.ProjectStorageRoot, true);
    }

    private void AddFwFile(string fileName)
    {
        File.WriteAllText(Path.Join(_cache.LangProject.LinkedFilesRootDir, fileName), "test");
    }

    private async Task AddDbFile(string fileName)
    {
        _lexBoxDbContext.Files.Add(new MediaFile()
        {
            Filename = Path.Join(
                Path.GetRelativePath(_cache.ProjectId.ProjectFolder, _cache.LangProject.LinkedFilesRootDir),
                fileName),
            ProjectId = _projectId
        });
        await _lexBoxDbContext.SaveChangesAsync();
    }

    private async Task AssertDbFileExists(string fileName)
    {
        var files = await _lexBoxDbContext.Files.Where(f => f.ProjectId == _projectId).ToArrayAsync();
        files.Should().Contain(f => f.Filename.EndsWith(fileName));
    }

    private async Task AssertDbFileDoesNotExists(string fileName)
    {
        var files = await _lexBoxDbContext.Files.Where(f => f.ProjectId == _projectId).ToArrayAsync();
        files.Should().NotContain(f => f.Filename.EndsWith(fileName));
    }

    [Fact]
    public async Task SyncingNothingWorks()
    {
        await _service.SyncMediaFiles(_cache);
    }

    [Fact]
    public async Task NewFwFilesGetAddedToDb()
    {
        AddFwFile("NewFile.txt");

        await _service.SyncMediaFiles(_cache);

        await AssertDbFileExists("NewFile.txt");
    }

    [Fact]
    public async Task FilesMissingFromFwGetRemoved()
    {
        await AddDbFile("NewDbFile.txt");

        await AssertDbFileExists("NewDbFile.txt");

        await _service.SyncMediaFiles(_cache);
        await AssertDbFileDoesNotExists("NewDbFile.txt");
    }

    [Fact]
    public async Task PreExistingFilesArePreserved()
    {
        AddFwFile("SomeFile.txt");
        await AddDbFile("SomeFile.txt");

        await _service.SyncMediaFiles(_cache);
        await AssertDbFileExists("SomeFile.txt");
    }
}
