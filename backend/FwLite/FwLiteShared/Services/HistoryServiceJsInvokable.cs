﻿using LcmCrdt;
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
    public async ValueTask<ProjectActivity[]> ProjectActivity()
    {
        return await historyService.ProjectActivity().ToArrayAsync();
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
}
