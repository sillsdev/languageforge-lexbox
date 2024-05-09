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
    private readonly string _basePath = Path.Join(Path.GetTempPath(), "HgServiceTests");
    private readonly HgConfig _hgConfig;
    private readonly HgService _hgService;

    public HgServiceTests()
    {
        _hgConfig = new HgConfig
        {
            RepoPath = Path.Join(_basePath, "hg-repos"),
            HgWebUrl = LexboxHgWeb,
            HgCommandServer = LexboxHgWeb + "/command/",
            HgResumableUrl = LexboxResumable,
            SendReceiveDomain = LexboxHgWeb
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
    [InlineData(HgType.hgWeb, LexboxHgWeb)]
    [InlineData(HgType.resumable, LexboxResumable)]
    public void DetermineProjectPrefixWorks(HgType type, string expectedUrl)
    {
        HgService.DetermineProjectUrlPrefix(type, _hgConfig).ShouldBe(expectedUrl);
    }

    // [Theory]
    // [InlineData(".hg/important-file.bin")]
    // [InlineData("unzip-test/.hg/important-file.bin")]
    public async Task CanFinishResetByUnZippingAnArchive(string filePath)
    {
        // arrange
        var code = "unzip-test";
        await _hgService.InitRepo(code);

        // act
        using var stream = new MemoryStream();
        using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            CreateSimpleEntry(zipArchive, filePath);
            CreateSimpleEntry(zipArchive, "random-subfolder/other-file.txt");
        }
        stream.Position = 0;
        await _hgService.FinishReset(code, stream);

        // assert
        var repoPath = Path.GetFullPath(Path.Join(_hgConfig.RepoPath, "u", code));
        Directory.EnumerateFiles(repoPath, "*", SearchOption.AllDirectories)
            .Select(p => Path.GetRelativePath(repoPath, p))
            .ShouldHaveSingleItem().ShouldBe(Path.Join(".hg", "important-file.bin"));
    }

    [Theory]
    [InlineData("-xy")]
    [InlineData("-x-y-z")]
    [InlineData("-123")]
    private async Task ProjectCodesMayNotStartWithHyphen(string code)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _hgService.InitRepo(code));
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

    // [Fact]
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
