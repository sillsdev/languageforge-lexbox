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
        return Task.Run(() => historyService.GetObject(commitId, entityId));
    }

    [JSInvokable]
    public Task<ProjectActivity[]> ProjectActivity(
        int skip,
        int take,
        string[]? authorFilterKeys = null,
        string[]? changeTypeKeys = null,
        ActivitySort sort = ActivitySort.NewestFirst)
    {
        return Task.Run(() => historyService.ProjectActivity(skip, take,
            new ActivityQuery(authorFilterKeys, changeTypeKeys, sort)));
    }

    [JSInvokable]
    public Task<ActivityAuthor[]> ListActivityAuthors()
    {
        return Task.Run(historyService.ListActivityAuthors);
    }

    [JSInvokable]
    public Task<ActivityChangeType[]> ListActivityChangeTypes()
    {
        return Task.Run(historyService.ListActivityChangeTypes);
    }

    [JSInvokable]
    public Task<ObjectSnapshot?> GetSnapshot(Guid snapshotId)
    {
        return Task.Run(() => historyService.GetSnapshot(snapshotId));
    }

    [JSInvokable]
    public Task<HistoryLineItem[]> GetHistory(Guid entityId)
    {
        return Task.Run(async () => await historyService.GetHistory(entityId).ToArrayAsync());
    }

    [JSInvokable]
    public Task<ChangeContext> LoadChangeContext(Guid commitId, int changeIndex)
    {
        return Task.Run(async () => await historyService.LoadChangeContext(commitId, changeIndex));
    }
}
