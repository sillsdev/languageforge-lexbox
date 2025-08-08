using MiniLcm.Models;
using SIL.Extensions;
using SystemTextJsonPatch;

namespace MiniLcm.SyncHelpers;

public static class WritingSystemSync
{
    public static async Task<int> Sync(WritingSystems beforeWritingSystems,
        WritingSystems afterWritingSystems,
        IMiniLcmApi api)
    {
        return await Sync(beforeWritingSystems.Vernacular, afterWritingSystems.Vernacular, api) +
               await Sync(beforeWritingSystems.Analysis, afterWritingSystems.Analysis, api);
    }
    public static async Task<int> Sync(WritingSystem[] beforeWritingSystems,
        WritingSystem[] afterWritingSystems,
        IMiniLcmApi api)
    {
        var writingSystemsDiffApi = new WritingSystemsDiffApi(api);
        return await DiffCollection.DiffOrderable(beforeWritingSystems, afterWritingSystems, writingSystemsDiffApi);
    }

    public static async Task<int> Sync(WritingSystem beforeWs, WritingSystem afterWs, IMiniLcmApi api)
    {
        var updateObjectInput = WritingSystemDiffToUpdate(beforeWs, afterWs);
        if (updateObjectInput is not null) await api.UpdateWritingSystem(afterWs.WsId, afterWs.Type, updateObjectInput);
        return updateObjectInput is null ? 0 : 1;
    }

    public static UpdateObjectInput<WritingSystem>? WritingSystemDiffToUpdate(WritingSystem beforeWritingSystem, WritingSystem afterWritingSystem)
    {
        JsonPatchDocument<WritingSystem> patchDocument = new();
        if (beforeWritingSystem.WsId != afterWritingSystem.WsId)
        {
            // TODO: Throw? Or silently ignore?
            throw new InvalidOperationException($"Tried to change immutable WsId from {beforeWritingSystem.WsId} to {afterWritingSystem.WsId}");
        }
        patchDocument.Operations.AddRange(SimpleStringDiff.GetStringDiff<WritingSystem>(nameof(WritingSystem.Name),
            beforeWritingSystem.Name,
            afterWritingSystem.Name));
        patchDocument.Operations.AddRange(SimpleStringDiff.GetStringDiff<WritingSystem>(nameof(WritingSystem.Abbreviation),
            beforeWritingSystem.Abbreviation,
            afterWritingSystem.Abbreviation));
        patchDocument.Operations.AddRange(SimpleStringDiff.GetStringDiff<WritingSystem>(nameof(WritingSystem.Font),
            beforeWritingSystem.Font,
            afterWritingSystem.Font));
        // TODO: Exemplars, Order, and do we need DeletedAt?
        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<WritingSystem>(patchDocument);
    }

    private class WritingSystemsDiffApi(IMiniLcmApi api) : IBaseOrderableCollectionDiffApi<WritingSystem, WritingSystemId>
    {
        public async Task<int> Add(WritingSystem ws, BetweenPosition<WritingSystemId> between)
        {
            //todo set order?
            await api.CreateWritingSystem(ws);
            return 1;
        }

        public Task<int> Remove(WritingSystem ws)
        {
            // await api.DeleteWritingSystem(beforeWs.Id); // Deleting writing systems is dangerous as it causes cascading data deletion. Needs careful thought.
            // TODO: should we throw an exception?
            return Task.FromResult(0);
        }

        public async Task<int> Move(WritingSystem ws, BetweenPosition<WritingSystemId> between)
        {
            await api.MoveWritingSystem(ws.WsId, ws.Type, between);
            return 1;
        }

        public Task<int> Replace(WritingSystem beforeWs, WritingSystem afterWs)
        {
            return Sync(beforeWs, afterWs, api);
        }

        public WritingSystemId GetId(WritingSystem value)
        {
            return value.WsId;
        }
    }
}
