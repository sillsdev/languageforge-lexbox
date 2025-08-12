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

    private class WritingSystemsDiffApi(IMiniLcmApi api) : IOrderableCollectionDiffApi<WritingSystemsDiffApi.OrderableWs>
    {
        private Dictionary<Guid, WritingSystemId> Mapping { get; } = new();
        public async Task<int> Diff(WritingSystem[] beforeWritingSystems, WritingSystem[] afterWritingSystems)
        {
            return await DiffCollection.DiffOrderable(
                //diff collection must work with a Guid, and the id's must match between the two lists
                //so we just generate the guids on the fly and make sure they're the same for the same wsId
                beforeWritingSystems.Select(ws => new OrderableWs(ws, GetOrderableId(ws.WsId))).OrderBy(o => o.Order).ToList(),
                afterWritingSystems.Select(ws => new OrderableWs(ws, GetOrderableId(ws.WsId))).OrderBy(o => o.Order).ToList(),
                this
            );
        }

        private Guid GetOrderableId(WritingSystemId wsId)
        {
            foreach (var kvp in Mapping)
            {
                if (kvp.Value == wsId)
                {
                    return kvp.Key;
                }
            }
            var newId = Guid.NewGuid();
            Mapping[newId] = wsId;
            return newId;
        }

        private class OrderableWs : IOrderable
        {
            public WritingSystem Ws { get; }
            //can't use Ws Guid because it is not set for FwData
            public Guid Id { get; }

            public double Order
            {
                get => Ws.Order;
                set => Ws.Order = value;
            }

            public OrderableWs(WritingSystem ws, Guid id)
            {
                this.Ws = ws;
                this.Id = id;
            }
        }

        async Task<int> IOrderableCollectionDiffApi<OrderableWs>.Add(OrderableWs value, BetweenPosition between)
        {
            var betweenWsId = new BetweenPosition<WritingSystemId?>(
                //we can't use `null` and must use new WritingSystemId?() to set a nullable value to null
                between.Previous is null ? new WritingSystemId?() : Mapping[between.Previous.Value],
                between.Next is null ? new WritingSystemId?() : Mapping[between.Next.Value]
                );
            await api.CreateWritingSystem(value.Ws, betweenWsId);
            return 1;
        }

        Task<int> IOrderableCollectionDiffApi<OrderableWs>.Remove(OrderableWs value)
        {
            // await api.DeleteWritingSystem(beforeWs.Id); // Deleting writing systems is dangerous as it causes cascading data deletion. Needs careful thought.
            // TODO: should we throw an exception?
            return Task.FromResult(0);
        }

        async Task<int> IOrderableCollectionDiffApi<OrderableWs>.Move(OrderableWs value, BetweenPosition between)
        {
            var betweenWsId = new BetweenPosition<WritingSystemId?>(
                //we can't use `null` and must use new WritingSystemId?() to set a nullable value to null
                between.Previous is null ? new WritingSystemId?() : Mapping[between.Previous.Value],
                between.Next is null ? new WritingSystemId?() : Mapping[between.Next.Value]
                );
            await api.MoveWritingSystem(value.Ws.WsId, value.Ws.Type, betweenWsId);
            return 1;
        }

        Task<int> IOrderableCollectionDiffApi<OrderableWs>.Replace(OrderableWs before, OrderableWs after)
        {
            return Sync(before.Ws, after.Ws, api);
        }
    }
}
