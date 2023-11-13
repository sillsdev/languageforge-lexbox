using System.IO.Compression;
using LexBoxApi.Services;
using LexCore.Config;
using LexCore.Entities;
using LexCore.Exceptions;
using LexSyncReverseProxy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Testing.Fixtures;

namespace Testing.LexCore.Services;

public class HgServiceTests
{
    private const string LexboxHgWeb = "https://hg.lexbox";
    private const string LexboxResumable = "https://resumable.lexbox";
    private const string RedmineResumable = "https://resumable.redmine";
    private const string RedminePublic = "https://public.redmine";
    private const string RedminePrivate = "https://private.redmine";
    private readonly string _basePath = Path.Join(Path.GetTempPath(), "HgServiceTests");
    private readonly HgConfig _hgConfig;
    private readonly HgService _hgService;

    public HgServiceTests()
    {
        _hgConfig = new HgConfig
        {
            RepoPath = Path.Join(_basePath, "hg-repos"),
            HgWebUrl = LexboxHgWeb,
            HgResumableUrl = LexboxResumable,
            PublicRedmineHgWebUrl = RedminePublic,
            PrivateRedmineHgWebUrl = RedminePrivate,
            RedmineHgResumableUrl = RedmineResumable,
            RedmineTrustToken = "tt"
        };
        _hgService = new HgService(new OptionsWrapper<HgConfig>(_hgConfig),
            Mock.Of<IHttpClientFactory>(),
            NullLogger<HgService>.Instance);
        CleanUpTempDir();
    }

    private void CleanUpTempDir()
    {
        if (Directory.Exists(_basePath))
        {
            Directory.Delete(_basePath, true);
        }
    }

    [Theory]
    //lexbox
    [InlineData(HgType.hgWeb, ProjectMigrationStatus.Migrated, LexboxHgWeb)]
    [InlineData(HgType.resumable, ProjectMigrationStatus.Migrated, LexboxResumable)]
    //redmine
    [InlineData(HgType.hgWeb, ProjectMigrationStatus.PublicRedmine, RedminePublic)]
    [InlineData(HgType.hgWeb, ProjectMigrationStatus.PrivateRedmine, RedminePrivate)]
    [InlineData(HgType.resumable, ProjectMigrationStatus.PublicRedmine, RedmineResumable)]
    [InlineData(HgType.resumable, ProjectMigrationStatus.PrivateRedmine, RedmineResumable)]
    public void DetermineProjectPrefixWorks(HgType type, ProjectMigrationStatus status, string expectedUrl)
    {
        HgService.DetermineProjectUrlPrefix(type, "test", status, _hgConfig).ShouldBe(expectedUrl);
    }

    [Theory]
    [InlineData(HgType.hgWeb)]
    [InlineData(HgType.resumable)]
    public void ThrowsIfMigrating(HgType type)
    {
        var act = () => HgService.DetermineProjectUrlPrefix(type, "test", ProjectMigrationStatus.Migrating, _hgConfig);
        act.ShouldThrow<ProjectMigratingException>();
    }

    [Theory]
    [InlineData(".hg/important-file.bin")]
    [InlineData("unzip-test/.hg/important-file.bin")]
    public async Task CanFinishResetByUnZippingAnArchive(string filePath)
    {
        var code = "unzip-test";
        await _hgService.InitRepo(code);
        var repoPath = Path.GetFullPath(Path.Join(_hgConfig.RepoPath, code));
        using var stream = new MemoryStream();
        using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            CreateSimpleEntry(zipArchive, filePath);
            CreateSimpleEntry(zipArchive, "random-subfolder/other-file.txt");
        }

        stream.Position = 0;
        await _hgService.FinishReset(code, stream);

        Directory.EnumerateFiles(repoPath, "*", SearchOption.AllDirectories)
            .Select(p => Path.GetRelativePath(repoPath, p))
            .ShouldHaveSingleItem().ShouldBe(Path.Join(".hg", "important-file.bin"));
    }

    private void CreateSimpleEntry(ZipArchive zipArchive, string filePath)
    {
        var entry = zipArchive.CreateEntry(filePath);
        using var fileStream = entry.Open();
        Span<byte> buff = stackalloc byte[100];
        Random.Shared.NextBytes(buff);
        fileStream.Write(buff);
        fileStream.Flush();
    }

    [Fact]
    public async Task ThrowsIfNoHgFolderIsFound()
    {
        var code = "unzip-test-no-hg";
        await _hgService.InitRepo(code);
        using var stream = new MemoryStream();
        using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            CreateSimpleEntry(zipArchive, "random-subfolder/other-file.txt");
        }

        stream.Position = 0;
        var act = () => _hgService.FinishReset(code, stream);
        act.ShouldThrow<ProjectResetException>();
    }
}
