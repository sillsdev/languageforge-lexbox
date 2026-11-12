using System.Text.Json;
using FwLiteShared.Events;
using FwLiteShared.LocalSync;
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
        services.AddScoped<LocalSyncService>();
        services.AddScoped<FolderSyncService>();
        services.AddScoped<MediaCopier>();
        services.AddSingleton<FolderProjectOpener>();
        services.AddSingleton<IPreferencesService, InMemoryPreferencesService>();
    }

    public static MiniLcmApiFixture CreateFixture(Guid projectId, bool seedWs = true) =>
        MiniLcmApiFixture.Create(seedWs, projectId, AddLocalSyncServices);

    public static async Task MakeEditor(MiniLcmApiFixture fixture, string userId)
    {
        var projectService = fixture.GetService<CurrentProjectService>();
        await projectService.UpdateLastUser(userId, userId);
        await projectService.UpdateUserRole(UserProjectRole.Editor);
    }

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
    public async Task AddRangeFromSync(IEnumerable<Commit> commits) =>
        await inner.AddRangeFromSync(LocalSyncTestHelpers.JsonRoundTrip(commits.ToArray(), options));

    public async Task<SyncState> GetSyncState() =>
        LocalSyncTestHelpers.JsonRoundTrip(await inner.GetSyncState(), options);

    public async Task<ChangesResult<Commit>> GetChanges(SyncState otherHeads) =>
        LocalSyncTestHelpers.JsonRoundTrip(await inner.GetChanges(otherHeads), options);

    public Task<SyncResults> SyncWith(ISyncable remoteModel) => remoteModel.SyncWith(this);
    public Task SyncMany(ISyncable[] remotes) => throw new NotSupportedException();
    public ValueTask<bool> ShouldSync() => ValueTask.FromResult(true);
}

public class InMemoryPreferencesService : IPreferencesService
{
    private readonly Dictionary<string, string> _values = [];
    public string? Get(string key) => _values.GetValueOrDefault(key);
    public void Set(string key, string value) => _values[key] = value;
    public void Remove(string key) => _values.Remove(key);
}
