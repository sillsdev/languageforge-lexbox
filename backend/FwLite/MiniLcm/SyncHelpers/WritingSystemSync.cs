using MiniLcm;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using SystemTextJsonPatch;

public static class WritingSystemSync
{
    public static async Task<int> Sync(WritingSystem[] currentWritingSystems,
        WritingSystem[] previousWritingSystems,
        IMiniLcmApi api)
    {
        return await DiffCollection.Diff(api,
            previousWritingSystems,
            currentWritingSystems,
            ws => ws.Id,
            async (api, currentWs) =>
            {
                await api.CreateWritingSystem(currentWs.Type, currentWs);
                return 1;
            },
            async (api, previousWs) =>
            {
                // await api.DeleteWritingSystem(previousWs.Id); // Deleting writing systems is dangerous as it causes cascading data deletion. Needs careful thought.
                // TODO: should we throw an exception?
                return 0;
            },
            async (api, previousWs, currentWs) =>
            {
                return await Sync(currentWs, previousWs, api);
            });
    }

    public static async Task<int> Sync(WritingSystem afterWs, WritingSystem beforeWs, IMiniLcmApi api)
    {
        var updateObjectInput = WritingSystemDiffToUpdate(beforeWs, afterWs);
        if (updateObjectInput is not null) await api.UpdateWritingSystem(afterWs.WsId, afterWs.Type, updateObjectInput);
        return updateObjectInput is null ? 0 : 1;
    }

    public static UpdateObjectInput<WritingSystem>? WritingSystemDiffToUpdate(WritingSystem previousWritingSystem, WritingSystem currentWritingSystem)
    {
        JsonPatchDocument<WritingSystem> patchDocument = new();
        patchDocument.Operations.AddRange(SimpleStringDiff.GetStringDiff<WritingSystem>(nameof(WritingSystem.WsId),
            previousWritingSystem.WsId,
            currentWritingSystem.WsId));
        patchDocument.Operations.AddRange(SimpleStringDiff.GetStringDiff<WritingSystem>(nameof(WritingSystem.Name),
            previousWritingSystem.Name,
            currentWritingSystem.Name));
        patchDocument.Operations.AddRange(SimpleStringDiff.GetStringDiff<WritingSystem>(nameof(WritingSystem.Abbreviation),
            previousWritingSystem.Abbreviation,
            currentWritingSystem.Abbreviation));
        patchDocument.Operations.AddRange(SimpleStringDiff.GetStringDiff<WritingSystem>(nameof(WritingSystem.Font),
            previousWritingSystem.Font,
            currentWritingSystem.Font));
        // TODO: Exemplars, Order, and do we need DeletedAt?
        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<WritingSystem>(patchDocument);
    }
}
