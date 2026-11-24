using LcmCrdt.Changes;
using LcmCrdt.Data;
using LcmCrdt.Harmony;
using LinqToDB.Async;
using MiniLcm.Exceptions;
using SIL.Harmony.Changes;

namespace LcmCrdt.MiniLcmImp;

public class CrdtPluginApi(
    MiniLcmRepositoryFactory repoFactory,
    HarmonyChangeWriter harmonyChangeWriter,
    CurrentProjectService projectService)
{
    public async IAsyncEnumerable<Plugin> GetPlugins()
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        await foreach (var plugin in repo.Plugins.AsAsyncEnumerable())
        {
            yield return plugin;
        }
    }

    public async Task<Plugin?> GetPlugin(Guid id)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.GetPlugin(id);
    }

    public async Task<Plugin> CreatePlugin(Plugin plugin)
    {
        AssertManagerRoleForPluginWrite();
        if (plugin.Id == Guid.Empty) plugin.Id = Guid.NewGuid();
        await harmonyChangeWriter.AddChange(new CreatePluginChange(plugin.Id, plugin));
        return await GetPlugin(plugin.Id) ?? throw NotFoundException.ForType<Plugin>(plugin.Id);
    }

    public async Task<Plugin> UpdatePlugin(Plugin plugin)
    {
        AssertManagerRoleForPluginWrite();
        await using var repo = await repoFactory.CreateRepoAsync();
        var id = plugin.Id;
        _ = await repo.GetPlugin(id) ?? throw NotFoundException.ForType<Plugin>(id);
        await harmonyChangeWriter.AddChange(new EditPluginChange(id, plugin));
        return await repo.GetPlugin(id) ?? throw NotFoundException.ForType<Plugin>(id);
    }

    public async Task DeletePlugin(Guid id)
    {
        AssertManagerRoleForPluginWrite();
        await using var repo = await repoFactory.CreateRepoAsync();
        _ = await repo.GetPlugin(id) ?? throw NotFoundException.ForType<Plugin>(id);
        await harmonyChangeWriter.AddChange(new DeleteChange<Plugin>(id));
    }

    private void AssertManagerRoleForPluginWrite()
    {
        if (projectService.ProjectData.Role == UserProjectRole.Manager) return;
        throw new UnauthorizedAccessException(
            $"Only managers can manage plugins.");
    }
}
