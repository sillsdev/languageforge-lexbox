using MiniLcm.Models;
using SystemTextJsonPatch;

namespace MiniLcm.SyncHelpers;

public static class WritingSystemSync
{
    public static async Task<int> Sync(WritingSystems previousWritingSystems,
        WritingSystems currentWritingSystems,
        IMiniLcmApi api)
    {
        return await Sync(previousWritingSystems.Vernacular, currentWritingSystems.Vernacular, api) +
               await Sync(previousWritingSystems.Analysis, currentWritingSystems.Analysis, api);
    }
    public static async Task<int> Sync(WritingSystem[] previousWritingSystems,
        WritingSystem[] currentWritingSystems,
        IMiniLcmApi api)
    {
        return await DiffCollection.Diff(api,
            previousWritingSystems,
            currentWritingSystems,
            ws => (ws.WsId, ws.Type),
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
                return await Sync(previousWs, currentWs, api);
            });
    }

    public static async Task<int> Sync(WritingSystem beforeWs, WritingSystem afterWs, IMiniLcmApi api)
    {
        var updateObjectInput = WritingSystemDiffToUpdate(beforeWs, afterWs);
        if (updateObjectInput is not null) await api.UpdateWritingSystem(afterWs.WsId, afterWs.Type, updateObjectInput);
        return updateObjectInput is null ? 0 : 1;
    }

    public static UpdateObjectInput<WritingSystem>? WritingSystemDiffToUpdate(WritingSystem previousWritingSystem, WritingSystem currentWritingSystem)
    {
        JsonPatchDocument<WritingSystem> patchDocument = new();
        if (previousWritingSystem.WsId != currentWritingSystem.WsId)
        {
            // TODO: Throw? Or silently ignore?
            throw new InvalidOperationException($"Tried to change immutable WsId from {previousWritingSystem.WsId} to {currentWritingSystem.WsId}");
        }
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
