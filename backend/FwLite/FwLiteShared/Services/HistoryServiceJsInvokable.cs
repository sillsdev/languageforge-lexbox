using LcmCrdt;
using Microsoft.JSInterop;
using MiniLcm.Models;
using SIL.Harmony.Db;

namespace FwLiteShared.Services;

public class HistoryServiceJsInvokable(HistoryService historyService)
{
    [JSInvokable]
    public Task<IObjectWithId> GetObject(Guid commitId, Guid entityId)
    {
        return historyService.GetObject(commitId, entityId);
    }

    [JSInvokable]
    public async Task<ProjectActivity[]> ProjectActivity(
        int skip,
        int take,
        string[]? authorFilterKeys = null,
        string[]? changeTypeKeys = null,
        ActivitySort sort = ActivitySort.NewestFirst)
    {
        return await historyService.ProjectActivity(skip, take,
            new ActivityQuery(authorFilterKeys, changeTypeKeys, sort));
    }

    [JSInvokable]
    public Task<ActivityAuthor[]> ListActivityAuthors()
    {
        return historyService.ListActivityAuthors();
    }

    [JSInvokable]
    public Task<ActivityChangeType[]> ListActivityChangeTypes()
    {
        return historyService.ListActivityChangeTypes();
    }

    [JSInvokable]
    public Task<ObjectSnapshot?> GetSnapshot(Guid snapshotId)
    {
        return historyService.GetSnapshot(snapshotId);
    }

    [JSInvokable]
    public async ValueTask<HistoryLineItem[]> GetHistory(Guid entityId)
    {
        return await historyService.GetHistory(entityId).ToArrayAsync();
    }

    [JSInvokable]
    public async ValueTask<ChangeContext> LoadChangeContext(Guid commitId, int changeIndex)
    {
        return await historyService.LoadChangeContext(commitId, changeIndex);
    }
}
