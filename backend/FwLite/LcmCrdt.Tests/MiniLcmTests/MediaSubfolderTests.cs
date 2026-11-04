using System.Text;
using LcmCrdt.MediaServer;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm.Media;

namespace LcmCrdt.Tests.MiniLcmTests;

public class MediaSubfolderTests(MiniLcmApiFixture fixture) : IClassFixture<MiniLcmApiFixture>
{
    private LcmMediaService MediaService => fixture.GetService<LcmMediaService>();

    private static MemoryStream Bytes(string content) => new(Encoding.UTF8.GetBytes(content));

    [Fact]
    public async Task SaveFile_KeepsTheLocalCopyAtTheCacheRoot()
    {
        var (resource, newResource) = await MediaService.SaveFile(Bytes("<html></html>"),
            new LcmFileMetadata("plugin-abc123.html", "text/html", LinkedFilesSubfolder: "Plugins"));

        newResource.Should().BeTrue();
        resource.LocalPath.Should().Be(Path.Combine(MediaService.ProjectResourceCachePath, "plugin-abc123.html"));
        resource.Metadata!.LinkedFilesSubfolder.Should().Be("Plugins");
    }

    [Fact]
    public async Task DeleteFile_ForgetsTheResourceAndItsLocalCopy()
    {
        var (resource, _) = await MediaService.SaveFile(Bytes("<html>old</html>"),
            new LcmFileMetadata("plugin-old.html", "text/html", LinkedFilesSubfolder: "Plugins"));
        var mediaUri = new MediaUri(resource.Id, "test.lexbox.org");

        await fixture.Api.DeleteFile(mediaUri);

        var read = await fixture.Api.GetFileStream(mediaUri, downloadIfMissing: false);
        read.Result.Should().Be(ReadFileResult.NotFound);
        (await MediaService.AllResources()).Should().NotContain(r => r.Id == resource.Id);
    }

    [Fact]
    public async Task DeleteFile_IgnoresNotFound()
    {
        var before = (await MediaService.AllResources()).Length;
        await fixture.Api.DeleteFile(MediaUri.NotFound);
        (await MediaService.AllResources()).Should().HaveCount(before);
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("spaced folder")]
    [InlineData("")]
    public async Task SaveFile_RejectsSubfoldersThatAreNotASingleSafeSegment(string subfolder)
    {
        var act = () => MediaService.SaveFile(Bytes("x"),
            new LcmFileMetadata("file.html", "text/html", LinkedFilesSubfolder: subfolder));

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
