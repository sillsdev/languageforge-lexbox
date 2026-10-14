using FwLiteShared.Events;
using LcmCrdt;
using LcmCrdt.Changes.Comments;
using LcmCrdt.Data;
using Microsoft.Extensions.Logging;
using MiniLcm.Models;
using SIL.Harmony;
using SIL.Harmony.Changes;

namespace FwLiteShared.Sync;

/// <summary>
/// What every sync does with the commits it received, whichever remote they came from (Lexbox, a local sync
/// host, or a joiner): mark incoming user comments unread and tell the UI which entries changed.
/// </summary>
public class SyncResultsProcessor(
    DataModel dataModel,
    CurrentProjectService currentProjectService,
    CurrentAuthorService currentAuthorService,
    ProjectEventBus changeEventBus,
    LocalCommentReadStatusService commentReadStatusService,
    ILogger<SyncResultsProcessor> logger)
{
    public async Task SendNotifications(SyncResults syncResults)
    {
        try
        {
            var deletedEntryIds = syncResults.MissingFromLocal
                .SelectMany(c => c.ChangeEntities, (_, change) => change.Change)
                .OfType<DeleteChange<Entry>>()
                .Select(c => c.EntityId)
                .ToHashSet();

            var changedEntryIds = new List<Guid>();
            await foreach (var entryId in syncResults.MissingFromLocal
                               .SelectMany(c => c.Snapshots, (commit, snapshot) => snapshot.Entity)
                               .ToAsyncEnumerable()
                               .SelectMany(e => GetEntryId(e.DbObject as IObjectWithId))
                               .Distinct())
            {
                if (entryId is null || deletedEntryIds.Contains(entryId.Value)) continue;
                changedEntryIds.Add(entryId.Value);
            }

            if (changedEntryIds.Count == 0 && deletedEntryIds.Count == 0) return;
            changeEventBus.PublishEntriesChanged(currentProjectService.Project, [.. changedEntryIds], [.. deletedEntryIds]);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to send notifications, continuing");
        }
    }

    public async Task ApplyCommentReadStatus(SyncResults syncResults)
    {
        var currentUserId = currentAuthorService.GetCurrentAuthor().Id;
        await commentReadStatusService.MarkCommentsUnread(GetUnreadCommentsFromSyncResults(syncResults, currentUserId));
        var (deletedCommentIds, deletedThreadIds) = GetDeletedCommentsFromSyncResults(syncResults);
        await commentReadStatusService.RemoveUnreadComments(deletedCommentIds);
        foreach (var threadId in deletedThreadIds)
        {
            await commentReadStatusService.MarkThreadRead(threadId);
        }
    }

    public static IEnumerable<(Guid CommentId, Guid CommentThreadId)> GetUnreadCommentsFromSyncResults(SyncResults syncResults, string? currentUserId)
    {
        return syncResults.MissingFromLocal
            .SelectMany(c => c.ChangeEntities, (_, change) => change.Change)
            .OfType<CreateUserCommentChange>()
            //with no current user (not signed in) nothing is "ours", so every synced comment is unread
            .Where(change => currentUserId is null || change.AuthorId != currentUserId)
            .Select(change => (CommentId: change.EntityId, change.CommentThreadId));
    }

    public static (IEnumerable<Guid> CommentIds, IEnumerable<Guid> ThreadIds) GetDeletedCommentsFromSyncResults(
        SyncResults syncResults)
    {
        var changes = syncResults.MissingFromLocal
            .SelectMany(c => c.ChangeEntities, (_, change) => change.Change);
        var commentIds = changes.OfType<DeleteChange<UserComment>>().Select(change => change.EntityId);
        var threadIds = changes.OfType<DeleteChange<CommentThread>>().Select(change => change.EntityId);
        return (commentIds, threadIds);
    }

    private async IAsyncEnumerable<Guid?> GetEntryId(IObjectWithId? entity)
    {
        switch (entity)
        {
            case Entry entry:
                yield return entry.Id;
                break;
            case Sense sense:
                yield return sense.EntryId;
                break;
            case ExampleSentence exampleSentence:
                yield return (await dataModel.GetLatest<Sense>(exampleSentence.SenseId))?.EntryId;
                break;
            case ComplexFormComponent complexFormComponent:
                yield return complexFormComponent.ComplexFormEntryId;
                yield return complexFormComponent.ComponentEntryId;
                break;
            default:
                break;
        }
    }
}
