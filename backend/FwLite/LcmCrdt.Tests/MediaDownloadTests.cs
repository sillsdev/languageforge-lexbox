using System.Net;
using System.Net.Http.Headers;
using System.Text;
using LcmCrdt.MediaServer;
using LcmCrdt.RemoteSync;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MiniLcm.Media;
using MiniLcm.Project;
using Refit;
using SIL.Harmony.Core;

namespace LcmCrdt.Tests;

public class MediaDownloadTests : IAsyncLifetime
{
    private readonly FakeMediaServerClient _server = new();
    private readonly MiniLcmApiFixture _fixture;

    public MediaDownloadTests()
    {
        _fixture = MiniLcmApiFixture.Create(configureServices: services =>
        {
            services.RemoveAll<IServerHttpClientProvider>();
            services.AddSingleton<IServerHttpClientProvider, OnlineHttpClientProvider>();
            services.RemoveAll<IRefitHttpServiceFactory>();
            services.AddSingleton<IRefitHttpServiceFactory>(new FakeMediaServerFactory(_server));
        });
    }

    public Task InitializeAsync() => _fixture.InitializeAsync();
    public Task DisposeAsync() => _fixture.DisposeAsync();

    private LcmMediaService MediaService => _fixture.GetService<LcmMediaService>();
    private string MediaFolder => MediaService.ProjectResourceCachePath;

    private async Task<string> Download()
    {
        IRemoteResourceService<LcmFileMetadata> remote = MediaService;
        return (await remote.DownloadResource(Guid.NewGuid().ToString(), MediaFolder)).LocalPath;
    }

    [Fact]
    public async Task ADownloadThatFailsPartWay_LeavesNoFile()
    {
        _server.Content = new StreamContent(new FailsAfterFirstRead("first half"));
        _server.FileName = "photo.jpg";

        var download = Download;

        await download.Should().ThrowAsync<IOException>();
        Directory.GetFiles(MediaFolder).Should().BeEmpty();
    }

    [Fact]
    public async Task ADownloadWithATakenName_KeepsTheExistingFile()
    {
        Directory.CreateDirectory(MediaFolder);
        var existingPath = Path.Combine(MediaFolder, "photo.jpg");
        await File.WriteAllTextAsync(existingPath, "existing");
        _server.Content = new StringContent("downloaded");
        _server.FileName = "photo.jpg";

        var downloadedPath = await Download();

        downloadedPath.Should().Be(Path.Combine(MediaFolder, "photo-1.jpg"));
        (await File.ReadAllTextAsync(downloadedPath)).Should().Be("downloaded");
        (await File.ReadAllTextAsync(existingPath)).Should().Be("existing");
        Directory.GetFiles(MediaFolder).Should().HaveCount(2);
    }

    private class FailsAfterFirstRead(string firstRead) : Stream
    {
        private readonly byte[] _firstRead = Encoding.UTF8.GetBytes(firstRead);
        private bool _read;

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_read) throw new IOException("connection lost");
            _read = true;
            _firstRead.CopyTo(buffer, offset);
            return _firstRead.Length;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private class OnlineHttpClientProvider : IServerHttpClientProvider
    {
        public ValueTask<HttpClient> GetHttpClient() => ValueTask.FromResult(new HttpClient());
        public ValueTask<ConnectionStatus> ConnectionStatus(bool forceRefresh = false) =>
            ValueTask.FromResult(MiniLcm.Project.ConnectionStatus.Online);
    }

    private class FakeMediaServerFactory(FakeMediaServerClient client) : IRefitHttpServiceFactory
    {
        public T Service<T>(HttpClient _)
        {
            if (typeof(T) != typeof(IMediaServerClient)) throw new NotSupportedException(typeof(T).Name);
            return (T)(object)client;
        }
    }

    private class FakeMediaServerClient : IMediaServerClient
    {
        public HttpContent Content { get; set; } = new StringContent("");
        public string FileName { get; set; } = "file";

        public Task<HttpResponseMessage> DownloadFile(Guid fileId)
        {
            Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = FileName };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = Content });
        }

        public Task<MediaUploadFileResponse> UploadFile(MultipartItem file, Guid projectId, string fileId,
            string? author = null, string? filename = null) => throw new NotSupportedException();
        public Task<LcmFileMetadata> GetFileMetadata(Guid fileId) => throw new NotSupportedException();
    }
}
