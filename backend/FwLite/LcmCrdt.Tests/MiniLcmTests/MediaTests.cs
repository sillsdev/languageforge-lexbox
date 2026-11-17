using System.Text;
using LcmCrdt.MediaServer;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm.Media;

namespace LcmCrdt.Tests.MiniLcmTests;

public class MediaTests : MediaTestsBase
{
    private readonly MiniLcmApiFixture _fixture = new();

    protected override async Task<IMiniLcmApi> NewApi()
    {
        await _fixture.InitializeAsync();
        return _fixture.Api;
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _fixture.DisposeAsync();
    }

    private LcmMediaService MediaService => _fixture.GetService<LcmMediaService>();

    [Fact]
    public async Task SaveFile_FileOnDiskWithoutAResource_SavesUnderANewName()
    {
        // e.g. left behind by an earlier failed save, or a LocalPath that no longer matches (#2693)
        var bytes = Encoding.UTF8.GetBytes("orphan");
        Directory.CreateDirectory(MediaService.ProjectResourceCachePath);
        var orphanPath = Path.Combine(MediaService.ProjectResourceCachePath, "orphan.txt");
        await File.WriteAllBytesAsync(orphanPath, bytes);

        var saved = await SaveTestFile("orphan.txt", bytes, "text/plain");

        saved.Result.Should().Be(UploadFileResult.SavedLocally);
        var resource = (await MediaService.AllResources()).Should().ContainSingle(r => r.Id == saved.MediaUri!.Value.FileId).Subject;
        resource.LocalPath.Should().NotBe(orphanPath);
        resource.Metadata!.Filename.Should().Be("orphan.txt");
        (await ReadAllBytes(saved.MediaUri!.Value)).Should().Equal(bytes);
    }

    [Fact]
    public async Task DeleteFile_ForgetsTheResourceAndItsLocalCopy()
    {
        var saved = await Api.SaveFile(new MemoryStream(Encoding.UTF8.GetBytes("<html>old</html>")),
            new LcmFileMetadata("plugin-old.html", "text/html"));
        var mediaUri = saved.MediaUri!.Value;

        await Api.DeleteFile(mediaUri);

        var read = await Api.GetFileStream(mediaUri, downloadIfMissing: false);
        read.Result.Should().Be(ReadFileResult.NotFound);
        (await MediaService.AllResources()).Should().NotContain(r => r.Id == mediaUri.FileId);
    }

    [Fact]
    public async Task DeleteFile_IgnoresNotFound()
    {
        var before = (await MediaService.AllResources()).Length;
        await Api.DeleteFile(MediaUri.NotFound);
        (await MediaService.AllResources()).Should().HaveCount(before);
    }
}
