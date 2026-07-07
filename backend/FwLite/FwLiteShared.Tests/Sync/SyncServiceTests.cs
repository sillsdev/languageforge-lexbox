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

        var unreadComments = SyncService.GetUnreadCommentsFromSyncResults(results, currentUserId: "current-user").ToArray();

        unreadComments.Should().ContainSingle()
            .Which.Should().Be((comment.Id, threadId));
    }

    [Fact]
    public void GetUnreadCommentsFromSyncResults_ExcludesCommentsAuthoredByCurrentUser()
    {
        var threadId = Guid.NewGuid();
        var mine = new CreateUserCommentChange(new UserComment
        {
            Id = Guid.NewGuid(),
            CommentThreadId = threadId,
            Text = "authored by me on another device",
            AuthorId = "current-user"
        });
        var theirs = new CreateUserCommentChange(new UserComment
        {
            Id = Guid.NewGuid(),
            CommentThreadId = threadId,
            Text = "authored by someone else",
            AuthorId = "other-user"
        });
        var commitId = Guid.NewGuid();
        var commit = new FakeCommit(commitId, new HybridDateTime(DateTimeOffset.UtcNow, 0))
        {
            ChangeEntities =
            [
                new ChangeEntity<IChange> { Change = mine, CommitId = commitId, EntityId = mine.EntityId, Index = 0 },
                new ChangeEntity<IChange> { Change = theirs, CommitId = commitId, EntityId = theirs.EntityId, Index = 1 }
            ]
        };

        var results = new SyncResults([commit], [], true);

        var unreadComments = SyncService.GetUnreadCommentsFromSyncResults(results, currentUserId: "current-user").ToArray();

        unreadComments.Should().ContainSingle("comments authored by other users are unread, the current user's own are not")
            .Which.Should().Be((theirs.EntityId, threadId));
    }

    [Fact]
    public void GetUnreadCommentsFromSyncResults_WithoutCurrentUser_IncludesCommentWithNullAuthor()
    {
        var threadId = Guid.NewGuid();
        var comment = new UserComment
        {
            Id = Guid.NewGuid(),
            CommentThreadId = threadId,
            Text = "synced comment with no author"
        };
        var change = new CreateUserCommentChange(comment);
        var commitId = Guid.NewGuid();
        var commit = new FakeCommit(commitId, new HybridDateTime(DateTimeOffset.UtcNow, 0))
        {
            ChangeEntities =
            [
                new ChangeEntity<IChange> { Change = change, CommitId = commitId, EntityId = change.EntityId, Index = 0 }
            ]
        };

        var results = new SyncResults([commit], [], true);

        var unreadComments = SyncService.GetUnreadCommentsFromSyncResults(results, currentUserId: null).ToArray();

        unreadComments.Should().ContainSingle("with no current user, even an author-less synced comment is unread")
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
