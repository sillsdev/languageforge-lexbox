using System.Text.Json;
using FwLiteShared.Events;
using FwLiteShared.KeepAwake;
using FwLiteShared.LocalSync;
using FwLiteShared.LocalSync.Hosting;
using FwLiteShared.Services;
using FwLiteShared.Sync;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SIL.Harmony.Core;

namespace LcmCrdt.Tests.LocalSync;

public static class LocalSyncTestHelpers
{
    public static void AddLocalSyncServices(IServiceCollection services)
    {
        services.AddSingleton<GlobalEventBus>();
        services.AddSingleton<ProjectEventBus>();
        services.AddScoped<SyncResultsProcessor>();
        services.AddScoped<LocalSyncHost>();
        services.AddScoped<LocalSyncService>();
        services.AddScoped<FolderSyncService>();
        services.AddScoped<MediaCopier>();
        services.AddSingleton<FolderProjectOpener>();
        services.AddSingleton<IPreferencesService, InMemoryPreferencesService>();
        services.AddSingleton<IKeepAwakePlatform, RecordingKeepAwakePlatform>();
        services.AddKeyedSingleton<IKeepAwakePlatform, RecordingKeepAwakePlatform>(LocalSyncHosting.KeepAwakeKey);
        services.AddSingleton<IKeepAwake, RefCountedKeepAwake>();
        // any free port: test classes host in parallel, and one taking another's stopped or moved port fakes a reachable host
        services.AddSingleton(sp =>
        {
            var hosting = ActivatorUtilities.CreateInstance<LocalSyncHosting>(sp);
            hosting.PreferredPort = 0;
            return hosting;
        });
        services.AddSingleton<IAccessPoint, FakeAccessPoint>();
        services.AddSingleton<INetworkNeighbors, FakeNetworkNeighbors>();
        services.AddScoped<LocalSyncJoiner>();
        services.AddScoped<LocalSyncJoinSession>();
        services.AddSingleton<ILocalNetworkAccess, RecordingLocalNetworkAccess>();
        services.AddSingleton<IDeviceName, TestDeviceName>();
    }

    public static MiniLcmApiFixture CreateFixture(Guid projectId, bool seedWs = true) =>
        MiniLcmApiFixture.Create(seedWs, projectId, AddLocalSyncServices);

    public static async Task MakeEditor(MiniLcmApiFixture fixture, string userId)
    {
        var projectService = fixture.GetService<CurrentProjectService>();
        await projectService.UpdateLastUser(userId, userId);
        await projectService.UpdateUserRole(UserProjectRole.Editor);
    }

    public static RecordingKeepAwakePlatform HostingKeepAwake(MiniLcmApiFixture fixture) =>
        (RecordingKeepAwakePlatform)fixture.GetService<IServiceProvider>().GetRequiredKeyedService<IKeepAwakePlatform>(LocalSyncHosting.KeepAwakeKey);

    public static JsonSerializerOptions JsonOptions(MiniLcmApiFixture fixture) =>
        fixture.GetService<IOptions<SIL.Harmony.Config.HarmonyConfig>>().Value.JsonSerializerOptions;

    public static T JsonRoundTrip<T>(T value, JsonSerializerOptions options) =>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, options), options)!;
}

/// <summary>
/// Another copy of the project, with every commit crossing a JSON boundary as it does on its way through a sync folder.
/// </summary>
public class OverJson(ISyncable inner, JsonSerializerOptions options) : ISyncable
{
    public async Task<Commit[]> AddRangeFromSync(IEnumerable<Commit> commits) =>
        await inner.AddRangeFromSync(LocalSyncTestHelpers.JsonRoundTrip(commits.ToArray(), options));

    public async Task<SyncState> GetSyncState() =>
        LocalSyncTestHelpers.JsonRoundTrip(await inner.GetSyncState(), options);

    public async Task<ChangesResult<Commit>> GetChanges(SyncState otherHeads) =>
        LocalSyncTestHelpers.JsonRoundTrip(await inner.GetChanges(otherHeads), options);

    public Task<SyncResults> SyncWith(ISyncable remoteModel) => remoteModel.SyncWith(this);
    public Task SyncMany(ISyncable[] remotes) => throw new NotSupportedException();
    public ValueTask<bool> ShouldSync() => ValueTask.FromResult(true);
}

public class TestDeviceName : IDeviceName
{
    public string Name => "Test device";
}

public class InMemoryPreferencesService : IPreferencesService
{
    private readonly Dictionary<string, string> _values = [];
    public string? Get(string key) => _values.GetValueOrDefault(key);
    public void Set(string key, string value) => _values[key] = value;
    public void Remove(string key) => _values.Remove(key);
}

/// <summary>
/// What a joiner talks to: the host's handlers, with every commit crossing a JSON boundary as it will over HTTP.
/// </summary>
public class HostOverTheWire(LocalSyncHost host, Guid projectId, JsonSerializerOptions options) : ISyncable
{
    // like the HTTP host, it reports nothing back, so all of them count as added
    public async Task<Commit[]> AddRangeFromSync(IEnumerable<Commit> commits)
    {
        var sent = commits.ToArray();
        await host.AddRange(projectId, LocalSyncTestHelpers.JsonRoundTrip(sent, options));
        return sent;
    }

    public async Task<SyncState> GetSyncState() =>
        LocalSyncTestHelpers.JsonRoundTrip(await host.GetSyncState(projectId), options);

    public async Task<ChangesResult<Commit>> GetChanges(SyncState otherHeads) =>
        LocalSyncTestHelpers.JsonRoundTrip(await host.GetChanges(projectId, otherHeads), options);

    public Task<SyncResults> SyncWith(ISyncable remoteModel) => remoteModel.SyncWith(this);
    public Task SyncMany(ISyncable[] remotes) => throw new NotSupportedException();
    public ValueTask<bool> ShouldSync() => ValueTask.FromResult(true);
}
