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
