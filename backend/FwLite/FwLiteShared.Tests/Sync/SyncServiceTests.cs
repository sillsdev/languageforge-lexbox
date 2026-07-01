using System.Diagnostics.CodeAnalysis;
using FwLiteShared.Sync;
using LcmCrdt.Changes.Comments;
using MiniLcm.Models;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;

namespace FwLiteShared.Tests.Sync;

public class SyncServiceTests
{
    [Fact]
    public void GetUnreadCommentsFromSyncResults_ReturnsCreatedUserComments()
    {
        var threadId = Guid.NewGuid();
        var comment = new UserComment
        {
            Id = Guid.NewGuid(),
            CommentThreadId = threadId,
            Text = "synced comment"
        };
        var commentChange = new CreateUserCommentChange(comment);
        var commitId = Guid.NewGuid();
        var commit = new FakeCommit(commitId, new HybridDateTime(DateTimeOffset.UtcNow, 0))
        {
            ChangeEntities =
            [
                new ChangeEntity<IChange>
                {
                    Change = commentChange,
                    CommitId = commitId,
                    EntityId = commentChange.EntityId,
                    Index = 0
                },
                new ChangeEntity<IChange>
                {
                    Change = new EditUserCommentChange(comment.Id, "edited", DateTimeOffset.UtcNow),
                    CommitId = commitId,
                    EntityId = comment.Id,
                    Index = 1
                }
            ]
        };

        var results = new SyncResults([commit], [], true);

        var unreadComments = SyncService.GetUnreadCommentsFromSyncResults(results).ToArray();

        unreadComments.Should().ContainSingle()
            .Which.Should().Be((comment.Id, threadId));
    }

    [Fact]
    public void GetDeletedCommentsFromSyncResults_ReturnsDeletedCommentsAndThreads()
    {
        var threadId = Guid.NewGuid();
        var commentId = Guid.NewGuid();
        var deletedThreadId = Guid.NewGuid();
        var commitId = Guid.NewGuid();
        var commit = new FakeCommit(commitId, new HybridDateTime(DateTimeOffset.UtcNow, 0))
        {
            ChangeEntities =
            [
                new ChangeEntity<IChange>
                {
                    Change = new DeleteChange<UserComment>(commentId),
                    CommitId = commitId,
                    EntityId = commentId,
                    Index = 0
                },
                new ChangeEntity<IChange>
                {
                    Change = new DeleteChange<CommentThread>(deletedThreadId),
                    CommitId = commitId,
                    EntityId = deletedThreadId,
                    Index = 1
                },
                new ChangeEntity<IChange>
                {
                    Change = new CreateUserCommentChange(new UserComment
                    {
                        Id = Guid.NewGuid(),
                        CommentThreadId = threadId,
                        Text = "still here"
                    }),
                    CommitId = commitId,
                    EntityId = Guid.NewGuid(),
                    Index = 2
                }
            ]
        };

        var results = new SyncResults([commit], [], true);

        var (deletedCommentIds, deletedThreadIds) = SyncService.GetDeletedCommentsFromSyncResults(results);

        deletedCommentIds.Should().ContainSingle().Which.Should().Be(commentId);
        deletedThreadIds.Should().ContainSingle().Which.Should().Be(deletedThreadId);
    }

    private class FakeCommit : Commit
    {
        [SetsRequiredMembers]
        public FakeCommit(Guid id, HybridDateTime hybridDateTime) : base(id, "", NullParentHash, hybridDateTime)
        {
            HybridDateTime = hybridDateTime;
            SetParentHash(NullParentHash);
        }
    }
}
