using System.Text;
using FwLiteShared.LocalSync;
using LcmCrdt.MediaServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MiniLcm.Media;
using SIL.Harmony.Core;
using static LcmCrdt.Tests.LocalSync.LocalSyncTestHelpers;

namespace LcmCrdt.Tests.LocalSync;

public class FolderMediaSyncTests : IAsyncLifetime
{
    private static readonly Guid ProjectId = Guid.Parse("8b0d2f4a-6c8e-4a1b-9d3f-5a7c9e1b3d79");
    private static readonly byte[] PictureBytes = Encoding.UTF8.GetBytes("pretend this is a jpeg of an apple");
    private readonly TempFolder _folder = new();
    private readonly TempFolder _aFiles = new();
    private readonly TempFolder _bFiles = new();
    private readonly MiniLcmApiFixture _a;
    private readonly MiniLcmApiFixture _b;

    public FolderMediaSyncTests()
    {
        // each device keeps its media cache under its own project directory
        _a = CreateDevice(_aFiles, seedWs: true);
        _b = CreateDevice(_bFiles, seedWs: false);
    }

    private static MiniLcmApiFixture CreateDevice(TempFolder files, bool seedWs) =>
        MiniLcmApiFixture.Create(seedWs, ProjectId, services =>
        {
            AddLocalSyncServices(services);
            services.Configure<LcmCrdtConfig>(config => config.ProjectPath = files.Path);
        });

    public async Task InitializeAsync()
    {
        await _a.InitializeAsync();
        await _b.InitializeAsync();
        await MakeEditor(_a, "user-a");
        await MakeEditor(_b, "user-b");
        await _b.DataModel.SyncWith(_a.DataModel);
    }

    public async Task DisposeAsync()
    {
        await _b.DisposeAsync();
        await _a.DisposeAsync();
        _folder.Dispose();
        _aFiles.Dispose();
        _bFiles.Dispose();
    }

    private static async Task<FolderSyncResult> SyncWithFolder(MiniLcmApiFixture device, string path) =>
        await device.GetService<FolderSyncService>().SyncWithFolder(path);

    private async Task<Guid> TakePictureOnA(string fileName = "apple.jpg")
    {
        // offline in tests, so like a picture taken in the village: pending upload to Lexbox
        var (resource, _) = await _a.GetService<LcmMediaService>()
            .SaveFile(new MemoryStream(PictureBytes), new LcmFileMetadata(fileName, "image/jpeg"));
        return resource.Id;
    }

    private static async Task<byte[]?> CachedBytes(MiniLcmApiFixture device, Guid resourceId)
    {
        var file = await device.GetService<LcmMediaService>().GetFileStream(resourceId, downloadIfMissing: false);
        if (file.Result != ReadFileResult.Success) return null;
        await using var stream = file.Stream!;
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy);
        return copy.ToArray();
    }

    [Fact]
    public async Task APictureTravelsToTheOtherDeviceThroughTheFolder()
    {
        var picture = await TakePictureOnA();

        await SyncWithFolder(_a, _folder.Path);
        await SyncWithFolder(_b, _folder.Path);

        (await CachedBytes(_b, picture)).Should().Equal(PictureBytes);
    }

    [Fact]
    public async Task APictureFromTheFolder_StaysPendingUploadAndCreatesNoCommit()
    {
        var picture = await TakePictureOnA();
        await SyncWithFolder(_a, _folder.Path);

        await SyncWithFolder(_b, _folder.Path);

        var onB = (await _b.GetService<LcmMediaService>().AllResources()).Single(r => r.Id == picture);
        onB.RemoteId.Should().BeNull("only Lexbox can say it's uploaded; it must still upload from here later");
        onB.Local.Should().BeTrue();
        // every commit b has is also in the folder: copying the file in made no commit of b's own
        var commitsOnB = (await _b.DataModel.GetChanges(new SyncState([]))).MissingFromClient.Select(c => c.Id);
        var commitsInFolder = (await new JsonSyncable(new DirectoryInfo(_folder.Path), JsonOptions(_b), NullLogger.Instance)
            .GetChanges(new SyncState([]))).MissingFromClient.Select(c => c.Id);
        commitsOnB.Should().BeEquivalentTo(commitsInFolder);
    }

    [Fact]
    public async Task EachFileIsCopiedOnce()
    {
        await TakePictureOnA();

        var aFirst = await SyncWithFolder(_a, _folder.Path);
        var bFirst = await SyncWithFolder(_b, _folder.Path);
        var aAgain = await SyncWithFolder(_a, _folder.Path);
        var bAgain = await SyncWithFolder(_b, _folder.Path);

        (aFirst.MediaSent, aFirst.MediaReceived).Should().Be((1, 0));
        (bFirst.MediaSent, bFirst.MediaReceived).Should().Be((0, 1));
        (aAgain.MediaSent, aAgain.MediaReceived).Should().Be((0, 0));
        (bAgain.MediaSent, bAgain.MediaReceived).Should().Be((0, 0));
    }

    [Fact]
    public async Task AFileThatFailsToCopy_DoesNotStopTheOthers()
    {
        var first = await TakePictureOnA("apple.jpg");
        var second = await TakePictureOnA("banana.jpg");
        await SyncWithFolder(_a, _folder.Path);
        await _b.DataModel.SyncWith(_a.DataModel);
        var brokenForFirst = new FailingFor(first, new FolderMediaStore(_folder.Path));

        var copied = await _b.GetService<MediaCopier>().PullAll(brokenForFirst);

        copied.Should().Be(new MediaCopyCounts(Copied: 1, Failed: 1));
        (await CachedBytes(_b, second)).Should().Equal(PictureBytes);
    }

    [Fact]
    public async Task APictureDeletedOnThisDevice_IsNotCopiedToAFolder()
    {
        var picture = await TakePictureOnA();
        await SyncWithFolder(_a, _folder.Path);
        await SyncWithFolder(_b, _folder.Path);
        // a deletes it; b learns of the deletion but still has its cached file
        await _a.GetService<LcmMediaService>().DeleteResource(picture);
        await _b.DataModel.SyncWith(_a.DataModel);
        using var freshFolder = new TempFolder();

        var result = await SyncWithFolder(_b, freshFolder.Path);

        result.MediaSent.Should().Be(0);
        Directory.Exists(Path.Combine(freshFolder.Path, "media")).Should().BeFalse();
    }

    private class FailingFor(Guid failingId, IMediaStore inner) : IMediaStore
    {
        public async Task<IReadOnlyCollection<Guid>> ResourceIds() => await inner.ResourceIds();
        public async Task<IReadOnlyCollection<Guid>> Wanted(IReadOnlyCollection<Guid> offered) => await inner.Wanted(offered);
        public async Task Put(Guid resourceId, Stream content, string fileName) => await inner.Put(resourceId, content, fileName);

        public async Task<StoredMedia?> Get(Guid resourceId)
        {
            if (resourceId == failingId) throw new IOException("the drive was pulled out");
            return await inner.Get(resourceId);
        }
    }
}
