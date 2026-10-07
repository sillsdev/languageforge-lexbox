using LcmCrdt.Changes;
using LcmCrdt.Data;
using LcmCrdt.Harmony;
using LinqToDB.Async;
using MiniLcm.Exceptions;
using SIL.Harmony.Changes;

namespace LcmCrdt.MiniLcmImp;

public class CrdtCustomViewApi(
    MiniLcmRepositoryFactory repoFactory,
    HarmonyChangeWriter harmonyChangeWriter,
    CurrentProjectService projectService)
{
    public async IAsyncEnumerable<CustomView> GetCustomViews()
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        await foreach (var customView in repo.CustomViews.AsAsyncEnumerable())
        {
            yield return customView;
        }
    }

    public async Task<CustomView?> GetCustomView(Guid id)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.GetCustomView(id);
    }

    public async Task<CustomView> CreateCustomView(CustomView customView)
    {
        AssertManagerRoleForCustomViewWrite();
        if (customView.Id == Guid.Empty) customView.Id = Guid.NewGuid();
        await harmonyChangeWriter.AddChange(new CreateCustomViewChange(customView.Id, customView));
        return await GetCustomView(customView.Id) ?? throw NotFoundException.ForType<CustomView>(customView.Id);
    }

    public async Task<CustomView> UpdateCustomView(CustomView customView)
    {
        AssertManagerRoleForCustomViewWrite();
        await using var repo = await repoFactory.CreateRepoAsync();
        var id = customView.Id;
        var _ = await repo.GetCustomView(id) ?? throw NotFoundException.ForType<CustomView>(id);
        await harmonyChangeWriter.AddChange(new EditCustomViewChange(id, customView));
        return await repo.GetCustomView(id) ?? throw NotFoundException.ForType<CustomView>(id);
    }

    public async Task DeleteCustomView(Guid id)
    {
        AssertManagerRoleForCustomViewWrite();
        await using var repo = await repoFactory.CreateRepoAsync();
        _ = await repo.GetCustomView(id) ?? throw NotFoundException.ForType<CustomView>(id);
        await harmonyChangeWriter.AddChange(new DeleteChange<CustomView>(id));
    }

    private void AssertManagerRoleForCustomViewWrite()
    {
        if (projectService.ProjectData.Role == UserProjectRole.Manager) return;
        throw new UnauthorizedAccessException(
            $"Only managers can manage custom views.");
    }
}
