using System.Net;
using System.Text;
using LcmCrdt.MediaServer;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm.Media;
using MiniLcm.Project;

namespace LcmCrdt.Tests.MediaServer;

public class MediaUploadTests : IAsyncLifetime
{
    private readonly RecordingHandler _server = new();
    private readonly MiniLcmApiFixture _fixture;

    public MediaUploadTests()
    {
        _fixture = MiniLcmApiFixture.Create(configureServices: services =>
            services.AddSingleton<IServerHttpClientProvider>(new OnlineClientProvider(_server)));
    }

    public Task InitializeAsync() => _fixture.InitializeAsync();
    public async Task DisposeAsync() => await _fixture.DisposeAsync();

    // The server then picks the LinkedFiles subfolder from the filename's extension
    // (Testing: MediaFileTests.UploadFile_WithoutContentType_GuessesFromFilename).
    [Theory]
    [InlineData("word.wav", "audio/wav")]
    [InlineData("photo.png", "image/png")]
    [InlineData("plugin-abc123.html", "text/html")]
    public async Task SaveFile_WhileOnline_UploadsWithoutAContentTypeSoTheServerGuessesFromTheFilename(string filename, string mimeType)
    {
        await _fixture.GetService<LcmMediaService>().SaveFile(new MemoryStream([1, 2, 3]), new LcmFileMetadata(filename, mimeType));

        _server.UploadedFilename.Should().Be(filename);
        _server.FileContentType.Should().BeNull();
    }

    private class OnlineClientProvider(HttpMessageHandler handler) : IServerHttpClientProvider
    {
        public ValueTask<HttpClient> GetHttpClient() =>
            ValueTask.FromResult(new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("http://lexbox.test") });

        public ValueTask<ConnectionStatus> ConnectionStatus(bool forceRefresh = false) =>
            ValueTask.FromResult(MiniLcm.Project.ConnectionStatus.Online);
    }

    private class RecordingHandler : HttpMessageHandler
    {
        public string? UploadedFilename { get; private set; }
        public string? FileContentType { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var filePart = ((MultipartFormDataContent)request.Content!)
                .Single(part => part.Headers.ContentDisposition?.Name?.Trim('"') == "file");
            UploadedFilename = filePart.Headers.ContentDisposition?.FileName?.Trim('"');
            FileContentType = filePart.Headers.ContentType?.MediaType;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($$"""{"guid":"{{Guid.NewGuid()}}"}""", Encoding.UTF8, "application/json")
            });
        }
    }
}
