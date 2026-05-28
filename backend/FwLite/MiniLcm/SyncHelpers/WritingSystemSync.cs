using MiniLcm.Models;
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
        return await writingSystemsDiffApi.Diff(beforeWritingSystems, afterWritingSystems);
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

    private class WritingSystemsDiffApi(IMiniLcmApi api) : IOrderableCollectionDiffApi<WritingSystem, WritingSystemId>
    {
        public async Task<int> Diff(WritingSystem[] beforeWritingSystems, WritingSystem[] afterWritingSystems)
        {
            return await DiffCollection.DiffOrderable(
                [.. beforeWritingSystems.OrderBy(ws => ws.Order)],
                [.. afterWritingSystems.OrderBy(ws => ws.Order)],
                this
            );
        }

        public WritingSystemId GetId(WritingSystem value)
        {
            return value.WsId;
        }

        public async Task<int> Add(WritingSystem value, BetweenPosition<WritingSystem> between)
        {
            var betweenWsId = new BetweenPosition<WritingSystemId?>(
                between.Previous is null ? new WritingSystemId?() : between.Previous.WsId,
                between.Next is null ? new WritingSystemId?() : between.Next.WsId
                );
            await api.CreateWritingSystem(value, betweenWsId);
            return 1;
        }

        public Task<int> Remove(WritingSystem value)
        {
            // await api.DeleteWritingSystem(beforeWs.Id); // Deleting writing systems is dangerous as it causes cascading data deletion. Needs careful thought.
            // TODO: should we throw an exception?
            return Task.FromResult(0);
        }

        public async Task<int> Move(WritingSystem value, BetweenPosition<WritingSystem> between)
        {
            var betweenWsId = new BetweenPosition<WritingSystemId?>(
                between.Previous is null ? new WritingSystemId?() : between.Previous.WsId,
                between.Next is null ? new WritingSystemId?() : between.Next.WsId
                );
            await api.MoveWritingSystem(value.WsId, value.Type, betweenWsId);
            return 1;
        }

        public Task<int> Replace(WritingSystem before, WritingSystem after)
        {
            return Sync(before, after, api);
        }
    }
}
