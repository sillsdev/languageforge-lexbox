using System.Linq.Expressions;
using CrdtLib.Changes;
using CrdtLib.Db;
using CrdtLib.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrdtLib;

/// <summary>
/// helper service to update snapshots and apply commits to them, has mutable state, don't reuse
/// </summary>
public class SnapshotWorker
{
    private readonly IReadOnlyDictionary<Guid, SimpleSnapshot>? _snapshots;
    private readonly CrdtRepository _crdtRepository;
    private readonly SimpleSnapshot? _oldestSnapshot;
    private readonly Dictionary<Guid, ObjectSnapshot> _pendingSnapshots  = [];
    private readonly List<ObjectSnapshot> _newIntermediateSnapshots = [];

    private SnapshotWorker(Dictionary<Guid, ObjectSnapshot> snapshots, CrdtRepository crdtRepository)
    {
        _pendingSnapshots = snapshots;
        var oldestSnapshot = snapshots.Values.MinBy(s => s.Commit.CompareKey);
        _oldestSnapshot = oldestSnapshot is null ? null : new SimpleSnapshot(oldestSnapshot);
        _crdtRepository = crdtRepository;
    }

    public static async Task<Dictionary<Guid, ObjectSnapshot>> ApplyCommitsToSnapshots(Dictionary<Guid, ObjectSnapshot> snapshots,
        CrdtRepository crdtRepository,
        ICollection<Commit> commits)
    {
        //we need to pass in the snapshots because we expect it to be modified, this is intended.
        //if the constructor makes a copy in the future this will need to be updated
        await new SnapshotWorker(snapshots, crdtRepository).ApplyCommitChanges(commits, false);
        return snapshots;
    }

    public SnapshotWorker(IReadOnlyDictionary<Guid, SimpleSnapshot> snapshots, CrdtRepository crdtRepository)
    {
        _snapshots = snapshots;
        _crdtRepository = crdtRepository;
        _oldestSnapshot = snapshots.Values.MinBy(s => (s.HybridDateTime.DateTime, s.HybridDateTime.Counter, s.CommitId));
    }

    public async Task UpdateSnapshots()
    {
        //need to use oldestCommitAppliedToAllSnapshots because some snapshots might not have changes that are newer
        //but before the oldestAddedCommit
        var commits = await _crdtRepository.CurrentCommits().Where(c =>
                _oldestSnapshot == null
                || (c.HybridDateTime.DateTime == _oldestSnapshot.HybridDateTime.DateTime &&
                    c.Id > _oldestSnapshot.CommitId)
                || c.HybridDateTime.DateTime > _oldestSnapshot.HybridDateTime.DateTime
                || (c.HybridDateTime.DateTime == _oldestSnapshot.HybridDateTime.DateTime 
                && c.HybridDateTime.Counter > _oldestSnapshot.HybridDateTime.Counter))
            .Include(c => c.ChangeEntities).ToArrayAsync();
        await ApplyCommitChanges(commits, true);
        
        //intermediate snapshots should be added first, as the last snapshot added for an entity will be used in the projected tables
        await _crdtRepository.AddIfNew(_newIntermediateSnapshots);
        await _crdtRepository.AddSnapshots(_pendingSnapshots.Values);
    }

    public async ValueTask ApplyCommitChanges(ICollection<Commit> commits, bool updateCommitHash)
    {
        var commitIndex = 0;
        var previousCommitHash = _oldestSnapshot?.CommitHash;
        foreach (var commit in commits)
        {
            if (updateCommitHash && previousCommitHash is not null)
            {
                //we're rewriting history, so we need to update the previous commit hash
                commit.SetParentHash(previousCommitHash);
            }

            previousCommitHash = commit.Hash;
            commitIndex++;
            foreach (var commitChange in commit.ChangeEntities)
            {
                IObjectBase entity;
                var snapshot = await GetSnapshot(commitChange.EntityId);
                var hasBeenApplied = snapshot?.CommitId == commit.Id;
                var changeContext = new ChangeContext(commit, this, _crdtRepository);
                bool wasDeleted;
                if (snapshot is not null)
                {
                    entity = snapshot.Entity.Copy();
                    wasDeleted = entity.DeletedAt.HasValue;
                }
                else
                {
                    entity = commitChange.Change.NewEntity(commit);
                    wasDeleted = false;
                }

                await commitChange.Change.ApplyChange(entity, changeContext);

                var deletedByChange = !wasDeleted && entity.DeletedAt.HasValue;
                if (deletedByChange)
                {
                    await MarkDeleted(entity.Id, commit);
                }
                //this snapshot has already been applied, we don't need to add it again
                //but we did need to run apply again because we may need to mark other entities as deleted
                if (hasBeenApplied) continue;

                //to get the state in a point in time we would have to find a snapshot before that time, then apply any commits that came after that snapshot but still before the point in time.
                //we would probably want the most recent snapshot to always follow current, so we might need to track the number of changes a given snapshot represents so we can 
                //decide when to create a new snapshot instead of replacing one inline. This would be done by using the current snapshots parent, instead of the snapshot itself.
                // s0 -> s1 -> sCurrent
                // if always taking snapshots would become
                // s0 -> s1 -> sCurrent -> sNew
                //but but to not snapshot every change we could do this instead
                // s0 -> s1 -> sNew

                //for now just skip every other change
                if (snapshot is not null && (snapshot.IsRoot || commitIndex % 2 == 0))
                {
                    _newIntermediateSnapshots.Add(snapshot);
                }

                var newSnapshot = new ObjectSnapshot(entity, commit, snapshot is null);
                AddSnapshot(newSnapshot);
            }
        }
    }

    /// <summary>
    /// responsible for removing references to the deleted entity from other entities
    /// </summary>
    /// <param name="deletedEntityId"></param>
    /// <param name="commit"></param>
    private async ValueTask MarkDeleted(Guid deletedEntityId, Commit commit)
    {
        Expression<Func<ObjectSnapshot, bool>> predicateExpression =
            snapshot => snapshot.References.Contains(deletedEntityId);
        var predicate = predicateExpression.Compile();

        var toRemoveRefFromIds = new HashSet<Guid>(await _crdtRepository.CurrentSnapshots()
            .Where(predicateExpression)
            .Select(s => s.EntityId)
            .ToArrayAsync());
        //snapshots from the db might be out of date, we want to use the most up to date data in the worker as well
        toRemoveRefFromIds.UnionWith(_pendingSnapshots.Values.Where(predicate).Select(s => s.EntityId));
        foreach (var entityId in toRemoveRefFromIds)
        {
            var snapshot = await GetSnapshot(entityId);
            if (snapshot is null) throw new NullReferenceException("unable to find snapshot for entity " + entityId);
            //could be different from what's in the db if a previous change has already updated it
            if (!predicate(snapshot)) continue;
            var hasBeenApplied = snapshot.CommitId == commit.Id;
            var updatedEntry = snapshot.Entity.Copy();
            var wasDeleted = updatedEntry.DeletedAt.HasValue;

            updatedEntry.RemoveReference(deletedEntityId, commit);
            var deletedByRemoveRef = !wasDeleted && updatedEntry.DeletedAt.HasValue;

            //this snapshot has already been applied, we don't need to add it again
            //but we did need to run apply again because we may need to mark other entities as deleted
            if (!hasBeenApplied)
                AddSnapshot(new ObjectSnapshot(updatedEntry, commit, false));

            //we need to do this after we add the snapshot above otherwise we might get stuck in a loop of deletions
            if (deletedByRemoveRef)
            {
                await MarkDeleted(updatedEntry.Id, commit);
            }
        }
    }

    public async ValueTask<ObjectSnapshot?> GetSnapshot(Guid entityId)
    {
        if (_pendingSnapshots.TryGetValue(entityId, out var snapshot))
        {
            return snapshot;
        }

        if (_snapshots?.TryGetValue(entityId, out var simpleSnapshot) == true)
        {
            return await _crdtRepository.FindSnapshot(simpleSnapshot.Id);
        }

        return null;
    }
    
    public void AddSnapshot(ObjectSnapshot snapshot)
    {
        //if there was already a pending snapshot there's no need to store it as both may point to the same commit
        _pendingSnapshots[snapshot.Entity.Id] = snapshot;
    }
}