using LcmCrdt.Changes;
using LcmCrdt.Data;
using LcmCrdt.Harmony;
using LinqToDB.Async;
using MiniLcm.Exceptions;
using MiniLcm.SyncHelpers;

namespace LcmCrdt.MiniLcmImp;

public class CrdtWritingSystemApi(MiniLcmRepositoryFactory repoFactory, HarmonyChangeWriter harmonyChangeWriter)
{
    public async Task<WritingSystems> GetWritingSystems()
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var systems = await repo.WritingSystemsOrdered.ToArrayAsync();
        return new WritingSystems
        {
            Analysis = [.. systems.Where(ws => ws.Type == WritingSystemType.Analysis)],
            Vernacular = [.. systems.Where(ws => ws.Type == WritingSystemType.Vernacular)]
        };
    }

    public async Task<WritingSystem?> GetWritingSystem(WritingSystemId id, WritingSystemType type)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.GetWritingSystem(id, type);
    }

    public async Task<WritingSystem> CreateWritingSystem(WritingSystem writingSystem, BetweenPosition<WritingSystemId?>? between = null)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var entityId = writingSystem.MaybeId ?? Guid.NewGuid();
        var wsType = writingSystem.Type;
        var exists = await repo.WritingSystems.AnyAsync(ws => ws.WsId == writingSystem.WsId && ws.Type == wsType);
        if (exists) throw new DuplicateObjectException($"Writing system {writingSystem.WsId.Code} ({wsType}) already exists");
        var betweenIds = between is null ? null : await between.MapAsync(async wsId => wsId is null ? null : (await repo.GetWritingSystem(wsId.Value, wsType))?.Id);
        var order = await OrderPicker.PickOrder(repo.WritingSystems.Where(ws => ws.Type == wsType), betweenIds);
        await harmonyChangeWriter.AddChange(new CreateWritingSystemChange(writingSystem, entityId, order));
        return await repo.GetWritingSystem(writingSystem.WsId, wsType) ?? throw NotFoundException.ForWs(writingSystem);
    }

    public async Task<WritingSystem> UpdateWritingSystem(WritingSystemId id, WritingSystemType type, UpdateObjectInput<WritingSystem> update)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var ws = await repo.GetWritingSystem(id, type) ?? throw NotFoundException.ForWs(id, type);
        var patchChange = new JsonPatchChange<WritingSystem>(ws.Id, update.Patch);
        await harmonyChangeWriter.AddChange(patchChange);
        return await repo.GetWritingSystem(id, type) ?? throw NotFoundException.ForWs(id, type);
    }

    public async Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after, IMiniLcmApi api)
    {
        await WritingSystemSync.Sync(before, after, api);
        return await GetWritingSystem(after.WsId, after.Type) ?? throw NotFoundException.ForWs(after);
    }

    public async Task MoveWritingSystem(WritingSystemId id, WritingSystemType type, BetweenPosition<WritingSystemId?> between)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var ws = await repo.GetWritingSystem(id, type) ?? throw NotFoundException.ForWs(id, type);
        var betweenIds = await between.MapAsync(async wsId => wsId is null ? null : (await repo.GetWritingSystem(wsId.Value, type))?.Id);
        var order = await OrderPicker.PickOrder(repo.WritingSystems.Where(s => s.Type == type), betweenIds);
        await harmonyChangeWriter.AddChange(new SetOrderChange<WritingSystem>(ws.Id, order));
    }
}
