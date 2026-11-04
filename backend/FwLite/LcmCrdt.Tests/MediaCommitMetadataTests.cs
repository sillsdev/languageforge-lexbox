using System.Text;
using LcmCrdt.MediaServer;
using LcmCrdt.RemoteSync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MiniLcm.Media;
using MiniLcm.Project;
using Refit;
using SIL.Harmony.Changes;
using SIL.Harmony.Resource;

namespace LcmCrdt.Tests;

/// <summary>
/// Media commits are written through Harmony's ResourceService rather than HarmonyChangeWriter,
/// so they must be handed the same author and client version that entry edits get.
/// </summary>
public class MediaCommitMetadataTests : IAsyncLifetime
{
    private const string UserName = "Media Test User";
    private const string UserId = "media-test-user-id";
    private readonly SwitchableHttpClientProvider _httpClientProvider = new();
    private readonly MiniLcmApiFixture _fixture;

    public MediaCommitMetadataTests()
    {
        _fixture = MiniLcmApiFixture.Create(configureServices: services =>
        {
            services.RemoveAll<IServerHttpClientProvider>();
            services.AddSingleton<IServerHttpClientProvider>(_httpClientProvider);
            services.RemoveAll<IRefitHttpServiceFactory>();
            services.AddSingleton<IRefitHttpServiceFactory, FakeMediaServerFactory>();
        }, isolateDbModel: false);
    }

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        await _fixture.GetService<CurrentProjectService>().UpdateLastUser(UserName, UserId);
    }

    public Task DisposeAsync() => _fixture.DisposeAsync();

    private LcmMediaService MediaService => _fixture.GetService<LcmMediaService>();

    [Fact]
    public async Task SavingAFileOffline_CommitCarriesAuthorAndClientVersion()
    {
        _httpClientProvider.Status = ConnectionStatus.Offline;

        await SaveFile("offline.txt");

        var commit = await SingleCommitWith<CreateRemoteResourcePendingUploadChange<LcmFileMetadata>>();
        AssertCurrentUser(commit);
    }

    [Fact]
    public async Task SavingAFileOnline_CommitCarriesAuthorAndClientVersion()
    {
        _httpClientProvider.Status = ConnectionStatus.Online;

        var (resource, _) = await SaveFile("online.txt");

        resource.Remote.Should().BeTrue("the fake server accepted the upload");
        var commit = await SingleCommitWith<CreateRemoteResourceChange<LcmFileMetadata>>();
        AssertCurrentUser(commit);
    }

    [Fact]
    public async Task UploadingAPendingFile_CommitCarriesAuthorAndClientVersion()
    {
        _httpClientProvider.Status = ConnectionStatus.Offline;
        await SaveFile("pending.txt");
        _httpClientProvider.Status = ConnectionStatus.Online;

        (await MediaService.UploadPendingResources()).Should().BeTrue();

        var commit = await SingleCommitWith<RemoteResourceUploadedChange<LcmFileMetadata>>();
        AssertCurrentUser(commit);
    }

    [Fact]
    public async Task DeletingAFile_CommitCarriesAuthorAndClientVersion()
    {
        _httpClientProvider.Status = ConnectionStatus.Offline;
        var (resource, _) = await SaveFile("to-delete.txt");

        await MediaService.DeleteResource(resource.Id);

        var commit = await SingleCommitWith<DeleteRemoteResourceChange<LcmFileMetadata>>();
        AssertCurrentUser(commit);
    }

    [Fact]
    public async Task AddingAnExistingRemoteFile_CommitCarriesAuthorAndClientVersion()
    {
        var localPath = Path.Combine(MediaService.ProjectResourceCachePath, "existing.txt");
        Directory.CreateDirectory(MediaService.ProjectResourceCachePath);
        await File.WriteAllTextAsync(localPath, "existing remote file");

        await MediaService.AddExistingRemoteResource(Guid.NewGuid(), localPath,
            new LcmFileMetadata("existing.txt", "text/plain"));

        var commit = await SingleCommitWith<CreateRemoteResourceChange<LcmFileMetadata>>();
        AssertCurrentUser(commit);
    }

    private async Task<(HarmonyResource<LcmFileMetadata> resource, bool newResource)> SaveFile(string fileName)
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes($"content of {fileName}"));
        return await MediaService.SaveFile(stream, new LcmFileMetadata(fileName, "text/plain"));
    }

    private async Task<Commit> SingleCommitWith<TChange>() where TChange : IChange
    {
        await using var dbContext = await _fixture.GetService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
        var commits = await dbContext.Set<Commit>().AsNoTracking().Include(c => c.ChangeEntities).ToArrayAsync();
        return commits.Should().ContainSingle(c => c.ChangeEntities.Any(ce => ce.Change is TChange)).Subject;
    }

    private static void AssertCurrentUser(Commit commit)
    {
        commit.Metadata.AuthorName.Should().Be(UserName);
        commit.Metadata.AuthorId.Should().Be(UserId);
        commit.Metadata.ClientVersion.Should().Be(AppVersion.Version);
    }

    private class SwitchableHttpClientProvider : IServerHttpClientProvider
    {
        public ConnectionStatus Status { get; set; } = MiniLcm.Project.ConnectionStatus.Offline;
        public ValueTask<HttpClient> GetHttpClient() => ValueTask.FromResult(new HttpClient());
        public ValueTask<ConnectionStatus> ConnectionStatus(bool forceRefresh = false) => ValueTask.FromResult(Status);
    }

    private class FakeMediaServerFactory : IRefitHttpServiceFactory
    {
        public T Service<T>(HttpClient client)
        {
            if (typeof(T) != typeof(IMediaServerClient)) throw new NotSupportedException(typeof(T).Name);
            return (T)(object)new FakeMediaServerClient();
        }
    }

    private class FakeMediaServerClient : IMediaServerClient
    {
        public Task<MediaUploadFileResponse> UploadFile(MultipartItem file, Guid projectId, string fileId,
            string? author = null, string? filename = null)
        {
            return Task.FromResult(new MediaUploadFileResponse(Guid.Parse(fileId), null));
        }

        public Task<LcmFileMetadata> GetFileMetadata(Guid fileId) => throw new NotSupportedException();
        public Task<HttpResponseMessage> DownloadFile(Guid fileId) => throw new NotSupportedException();
    }
}
