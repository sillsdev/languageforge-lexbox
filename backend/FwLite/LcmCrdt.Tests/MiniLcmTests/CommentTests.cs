using FluentValidation;
using LcmCrdt.Data;
using Microsoft.EntityFrameworkCore;

namespace LcmCrdt.Tests.MiniLcmTests;

public class CommentTests(MiniLcmApiFixture fixture) : IClassFixture<MiniLcmApiFixture>
{
    private async Task SetCurrentUser(string userId, string? name = null)
    {
        var projectService = fixture.GetService<CurrentProjectService>();
        await projectService.UpdateLastUser(name ?? userId, userId);
        await projectService.UpdateUserRole(UserProjectRole.Editor);
    }

    private Task MarkCommentsUnread(params UserComment[] comments)
    {
        var readStatusService = fixture.GetService<LocalCommentReadStatusService>();
        return readStatusService.MarkCommentsUnread(comments.Select(comment => (comment.Id, comment.CommentThreadId)));
    }

    private Task ClearUnreadComments()
    {
        return fixture.Api.MarkAllCommentsRead();
    }

    private async Task ClearLastUserId()
    {
        await fixture.DbContext.ProjectData.ExecuteUpdateAsync(calls => calls
            .SetProperty(p => p.LastUserId, (string?)null)
            .SetProperty(p => p.LastUserName, (string?)null));
        await fixture.GetService<CurrentProjectService>().RefreshProjectData();
    }

    private static CommentThread NewThread(SubjectType subjectType = SubjectType.Entry, Guid? subjectId = null)
    {
        return new CommentThread
        {
            Id = Guid.NewGuid(),
            SubjectType = subjectType,
            SubjectId = subjectId ?? Guid.NewGuid()
        };
    }

    private static UserComment NewComment(string text = "Comment", Guid? id = null)
    {
        return new UserComment
        {
            Id = id ?? Guid.NewGuid(),
            Text = text
        };
    }

    private async Task<(CommentThread Thread, UserComment FirstComment)> CreateThreadWithComment(
        string authorId,
        SubjectType subjectType = SubjectType.Entry,
        Guid? subjectId = null,
        string text = "First")
    {
        await SetCurrentUser(authorId);
        var thread = await fixture.Api.CreateCommentThread(NewThread(subjectType, subjectId), NewComment(text));
        var firstComment = (await fixture.Api.GetUserComments(thread.Id).ToArrayAsync()).Single();
        return (thread, firstComment);
    }

    [Fact]
    public async Task CreateThreadWithFirstComment_PopulatesAuthorAndTimestamps()
    {
        var authorId = $"author-{Guid.NewGuid()}";
        await SetCurrentUser(authorId, "Author Name");

        var thread = await fixture.Api.CreateCommentThread(NewThread(), NewComment("hello"));
        var comments = await fixture.Api.GetUserComments(thread.Id).ToArrayAsync();

        thread.Status.Should().Be(ThreadStatus.Open);
        thread.CreatedAt.Should().NotBe(default);
        thread.AuthorId.Should().Be(authorId);
        thread.AuthorName.Should().Be("Author Name");
        comments.Should().ContainSingle();
        comments[0].Text.Should().Be("hello");
        comments[0].AuthorId.Should().Be(authorId);
        comments[0].AuthorName.Should().Be("Author Name");
        comments[0].CreatedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task CreateThreadWithFirstComment_ThreadAndCommentShareAuthor()
    {
        var authorId = $"author-{Guid.NewGuid()}";
        await SetCurrentUser(authorId, "Thread Author");

        var thread = await fixture.Api.CreateCommentThread(NewThread(), NewComment("hello"));
        var reloaded = await fixture.Api.GetCommentThread(thread.Id);

        reloaded.Should().NotBeNull();
        reloaded!.AuthorId.Should().Be(authorId);
        reloaded.AuthorName.Should().Be("Thread Author");
    }

    [Fact]
    public async Task RepliesEditStatusAndDeleteComment_Work()
    {
        var authorId = $"author-{Guid.NewGuid()}";
        var (thread, firstComment) = await CreateThreadWithComment(authorId);

        var reply = await fixture.Api.AddUserComment(thread.Id, NewComment("reply") with
        {
            PreviousCommentId = firstComment.Id
        });
        var edited = await fixture.Api.EditUserComment(reply.Id, "edited reply");
        var closed = await fixture.Api.SetCommentThreadStatus(thread.Id, ThreadStatus.Closed);
        await fixture.Api.DeleteUserComment(reply.Id);

        edited.Text.Should().Be("edited reply");
        edited.UpdatedAt.Should().BeAfter(edited.CreatedAt);
        closed.Status.Should().Be(ThreadStatus.Closed);
        var remaining = await fixture.Api.GetUserComments(thread.Id).ToArrayAsync();
        remaining.Should().ContainSingle(c => c.Id == firstComment.Id);
    }

    [Fact]
    public async Task GetCommentThreads_CanIncludeComments()
    {
        var authorId = $"author-{Guid.NewGuid()}";
        var subjectId = Guid.NewGuid();
        var (thread, firstComment) = await CreateThreadWithComment(authorId, SubjectType.Entry, subjectId);
        var reply = await fixture.Api.AddUserComment(thread.Id, NewComment("reply") with
        {
            PreviousCommentId = firstComment.Id
        });
        var (_, otherComment) = await CreateThreadWithComment(authorId, SubjectType.Entry, Guid.NewGuid(), "other");

        var withoutComments = await fixture.Api.GetCommentThreads(SubjectType.Entry, subjectId).ToArrayAsync();
        var withComments = await fixture.Api.GetCommentThreads(SubjectType.Entry, subjectId, includeComments: true).ToArrayAsync();

        withoutComments.Should().ContainSingle();
        withoutComments[0].Comments.Should().BeNull();
        withComments.Should().ContainSingle();
        var includedComments = withComments[0].Comments;
        includedComments.Should().NotBeNull();
        includedComments!.Should().Contain(c => c.Id == firstComment.Id);
        includedComments.Should().Contain(c => c.Id == reply.Id);
        includedComments.Should().NotContain(c => c.Id == otherComment.Id);
    }

    [Fact]
    public async Task DeleteThread_CascadesToComments()
    {
        var (thread, _) = await CreateThreadWithComment($"author-{Guid.NewGuid()}");
        await fixture.Api.AddUserComment(thread.Id, NewComment("reply"));

        await fixture.Api.DeleteCommentThread(thread.Id);

        (await fixture.Api.GetCommentThread(thread.Id)).Should().BeNull();
        (await fixture.Api.GetUserComments(thread.Id).ToArrayAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task DeletingParentEntry_DoesNotDeleteThreadOrComments()
    {
        var authorId = $"author-{Guid.NewGuid()}";
        await SetCurrentUser(authorId);
        var entry = await fixture.Api.CreateEntry(new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "commented entry" } }
        });
        var (thread, firstComment) = await CreateThreadWithComment(authorId, SubjectType.Entry, entry.Id);

        await fixture.Api.DeleteEntry(entry.Id);

        (await fixture.Api.GetCommentThread(thread.Id)).Should().NotBeNull();
        (await fixture.Api.GetUserComment(firstComment.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task CanCreateThreadForAlreadyDeletedSubject()
    {
        var authorId = $"author-{Guid.NewGuid()}";
        await SetCurrentUser(authorId);
        var entry = await fixture.Api.CreateEntry(new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "deleted entry" } }
        });
        await fixture.Api.DeleteEntry(entry.Id);

        var thread = await fixture.Api.CreateCommentThread(NewThread(SubjectType.Entry, entry.Id), NewComment("after delete"));

        thread.SubjectId.Should().Be(entry.Id);
        (await fixture.Api.GetUserComments(thread.Id).ToArrayAsync()).Should().ContainSingle();
    }

    [Fact]
    public async Task ClosedThread_AllowsRepliesAndUnreadTracking()
    {
        var firstAuthor = $"author-{Guid.NewGuid()}";
        var secondAuthor = $"author-{Guid.NewGuid()}";
        var (thread, _) = await CreateThreadWithComment(firstAuthor);
        await fixture.Api.SetCommentThreadStatus(thread.Id, ThreadStatus.Closed);

        await SetCurrentUser(secondAuthor);
        var reply = await fixture.Api.AddUserComment(thread.Id, NewComment("late reply"));
        await MarkCommentsUnread(reply);

        await SetCurrentUser(firstAuthor);
        var comments = await fixture.Api.GetUserComments(thread.Id).ToArrayAsync();
        var unread = await fixture.Api.GetUnreadComments(thread.Id).ToArrayAsync();

        comments.Should().Contain(c => c.Id == reply.Id);
        unread.Should().ContainSingle(c => c.Id == reply.Id);
    }

    [Fact]
    public async Task SupportsAllSubjectTypes()
    {
        var authorId = $"author-{Guid.NewGuid()}";
        foreach (var subjectType in Enum.GetValues<SubjectType>())
        {
            var thread = await CreateThreadWithComment(authorId, subjectType, Guid.NewGuid(), subjectType.ToString());
            thread.Thread.SubjectType.Should().Be(subjectType);
        }
    }

    [Fact]
    public async Task OnlyAuthorCanEditOrDeleteComment()
    {
        var authorId = $"author-{Guid.NewGuid()}";
        var otherUserId = $"other-{Guid.NewGuid()}";
        var (_, firstComment) = await CreateThreadWithComment(authorId);

        await SetCurrentUser(otherUserId);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Api.EditUserComment(firstComment.Id, "not allowed"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Api.DeleteUserComment(firstComment.Id));
    }

    [Fact]
    public async Task ReadStatus_CanMarkOneThreadAndAll()
    {
        var readerId = $"reader-{Guid.NewGuid()}";
        var authorId = $"author-{Guid.NewGuid()}";
        await ClearUnreadComments();
        var (thread1, firstComment1) = await CreateThreadWithComment(authorId);
        var reply1 = await fixture.Api.AddUserComment(thread1.Id, NewComment("reply 1"));
        var (thread2, firstComment2) = await CreateThreadWithComment(authorId);
        await MarkCommentsUnread(firstComment1, reply1, firstComment2);

        await SetCurrentUser(readerId);
        (await fixture.Api.CountUnreadComments()).Should().Be(3);

        await fixture.Api.MarkCommentRead(firstComment1.Id);
        (await fixture.Api.CountUnreadComments()).Should().Be(2);
        (await fixture.Api.CountUnreadComments(thread1.Id)).Should().Be(1);

        await fixture.Api.MarkCommentThreadRead(thread1.Id);
        (await fixture.Api.CountUnreadComments()).Should().Be(1);
        (await fixture.Api.GetUnreadComments().ToArrayAsync()).Should().ContainSingle(c => c.Id == firstComment2.Id);
        (await fixture.Api.CountUnreadComments(thread2.Id)).Should().Be(1);

        await fixture.Api.MarkAllCommentsRead();
        (await fixture.Api.CountUnreadComments()).Should().Be(0);
        (await fixture.Api.GetUnreadComments().ToArrayAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task ReadStatus_CanGetUnreadCommentsBySubject()
    {
        var readerId = $"reader-{Guid.NewGuid()}";
        var authorId = $"author-{Guid.NewGuid()}";
        var subjectId = Guid.NewGuid();
        await ClearUnreadComments();
        var (_, firstComment) = await CreateThreadWithComment(authorId, SubjectType.Entry, subjectId, "target 1");
        var (_, secondComment) = await CreateThreadWithComment(authorId, SubjectType.Entry, subjectId, "target 2");
        var (_, otherSubjectComment) = await CreateThreadWithComment(authorId, SubjectType.Entry, Guid.NewGuid(), "other subject");
        var (_, otherTypeComment) = await CreateThreadWithComment(authorId, SubjectType.Sense, subjectId, "other type");
        await MarkCommentsUnread(firstComment, secondComment, otherSubjectComment, otherTypeComment);

        await SetCurrentUser(readerId);
        var unread = await fixture.Api.GetUnreadCommentsForSubject(SubjectType.Entry, subjectId).ToArrayAsync();

        unread.Should().HaveCount(2);
        unread.Should().Contain(c => c.Id == firstComment.Id);
        unread.Should().Contain(c => c.Id == secondComment.Id);
        unread.Should().NotContain(c => c.Id == otherSubjectComment.Id);
        unread.Should().NotContain(c => c.Id == otherTypeComment.Id);
    }

    [Fact]
    public async Task ReadStatus_IsLocalNotPerUser()
    {
        var reader1 = $"reader-{Guid.NewGuid()}";
        var reader2 = $"reader-{Guid.NewGuid()}";
        var authorId = $"author-{Guid.NewGuid()}";
        await ClearUnreadComments();
        var (_, firstComment) = await CreateThreadWithComment(authorId);
        await MarkCommentsUnread(firstComment);

        await SetCurrentUser(reader1);
        await fixture.Api.MarkCommentRead(firstComment.Id);

        await SetCurrentUser(reader2);
        (await fixture.Api.GetUnreadComments().ToArrayAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task MarkCommentsUnread_ConcurrentDuplicate_DoesNotThrowAndLeavesSingleRow()
    {
        var authorId = $"author-{Guid.NewGuid()}";
        await ClearUnreadComments();
        var (thread, firstComment) = await CreateThreadWithComment(authorId);
        var readStatusService = fixture.GetService<LocalCommentReadStatusService>();
        var commentRef = (firstComment.Id, firstComment.CommentThreadId);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => readStatusService.MarkCommentsUnread([commentRef]))
            .ToArray();

        await Task.WhenAll(tasks);

        fixture.DbContext.UnreadComments.Count(c => c.CommentId == firstComment.Id).Should().Be(1);
        (await readStatusService.CountUnreadComments(thread.Id)).Should().Be(1);
    }

    [Fact]
    public async Task MarkCommentsUnread_BatchWithPartialDuplicate_InsertsRemainingNewComments()
    {
        var authorId = $"author-{Guid.NewGuid()}";
        await ClearUnreadComments();
        var (_, comment1) = await CreateThreadWithComment(authorId, text: "one");
        var (_, comment2) = await CreateThreadWithComment(authorId, text: "two");
        var (_, comment3) = await CreateThreadWithComment(authorId, text: "three");
        var readStatusService = fixture.GetService<LocalCommentReadStatusService>();
        var batch = new[]
        {
            (comment1.Id, comment1.CommentThreadId),
            (comment2.Id, comment2.CommentThreadId),
            (comment3.Id, comment3.CommentThreadId)
        };

        await Task.WhenAll(
            readStatusService.MarkCommentsUnread(batch),
            readStatusService.MarkCommentsUnread([(comment1.Id, comment1.CommentThreadId)]));

        (await readStatusService.CountUnreadComments()).Should().Be(3);
        fixture.DbContext.UnreadComments.Select(c => c.CommentId)
            .Should().BeEquivalentTo([comment1.Id, comment2.Id, comment3.Id]);
    }

    [Fact]
    public async Task CreateCommentThread_WithoutLastUserId_ThrowsValidationException()
    {
        await ClearLastUserId();

        var act = () => fixture.Api.CreateCommentThread(NewThread(), NewComment("hello"));

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*known user identity*");
    }

    [Fact]
    public async Task AddUserComment_WithoutLastUserId_ThrowsValidationException()
    {
        var authorId = $"author-{Guid.NewGuid()}";
        var (thread, _) = await CreateThreadWithComment(authorId);
        await ClearLastUserId();

        var act = () => fixture.Api.AddUserComment(thread.Id, NewComment("reply"));

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*known user identity*");
    }

    [Fact]
    public async Task ReadStatus_IncomingCommentsBecomeUnreadAndMarkingIsIdempotent()
    {
        var readerId = $"reader-{Guid.NewGuid()}";
        var authorId = $"author-{Guid.NewGuid()}";
        await ClearUnreadComments();
        var (thread, _) = await CreateThreadWithComment(authorId);

        await SetCurrentUser(authorId);
        var comment = await fixture.Api.AddUserComment(thread.Id, NewComment("arrived later"));
        await MarkCommentsUnread(comment);
        await MarkCommentsUnread(comment);

        await SetCurrentUser(readerId);
        (await fixture.Api.GetUnreadComments(thread.Id).ToArrayAsync()).Should().ContainSingle(c => c.Id == comment.Id);

        await fixture.Api.MarkCommentRead(comment.Id);
        await fixture.Api.MarkCommentRead(comment.Id);

        (await fixture.Api.GetUnreadComments(thread.Id).ToArrayAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task ReadStatus_LocalCreatedCommentsStartRead()
    {
        var authorId = $"author-{Guid.NewGuid()}";
        await ClearUnreadComments();
        var (thread, firstComment) = await CreateThreadWithComment(authorId);
        await fixture.Api.AddUserComment(thread.Id, NewComment("reply"));

        (await fixture.Api.GetUnreadComments().ToArrayAsync()).Should().BeEmpty();
        fixture.DbContext.UnreadComments.Should().BeEmpty();
        (await fixture.Api.GetUserComment(firstComment.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task GetCommentThreads_CommentsAreSortedChronologically()
    {
        var authorId = $"author-{Guid.NewGuid()}";
        await SetCurrentUser(authorId);
        var subjectId = Guid.NewGuid();
        var baseTime = DateTimeOffset.UtcNow.AddHours(-5);
        var threadId = Guid.NewGuid();
        var firstCommentId = Guid.NewGuid();
        var secondCommentId = Guid.NewGuid();
        var thirdCommentId = Guid.NewGuid();

        await fixture.Api.CreateCommentThread(
            NewThread(SubjectType.Entry, subjectId) with { Id = threadId },
            NewComment("first") with
            {
                Id = firstCommentId,
                CreatedAt = baseTime,
                UpdatedAt = baseTime
            });
        await fixture.Api.AddUserComment(threadId, NewComment("third") with
        {
            Id = thirdCommentId,
            CreatedAt = baseTime.AddHours(2),
            UpdatedAt = baseTime.AddHours(2)
        });
        await fixture.Api.AddUserComment(threadId, NewComment("second") with
        {
            Id = secondCommentId,
            CreatedAt = baseTime.AddHours(1),
            UpdatedAt = baseTime.AddHours(1)
        });

        var thread = (await fixture.Api.GetCommentThreads(SubjectType.Entry, subjectId, includeComments: true)
            .ToArrayAsync()).Single();
        var commentIds = thread.Comments!.Select(c => c.Id).ToArray();

        commentIds.Should().Equal([firstCommentId, secondCommentId, thirdCommentId]);
    }

    [Fact]
    public async Task ReadStatus_DeletedThreadRemovesUnreadRows()
    {
        var authorId = $"author-{Guid.NewGuid()}";
        await ClearUnreadComments();
        var (thread, firstComment) = await CreateThreadWithComment(authorId);
        var reply = await fixture.Api.AddUserComment(thread.Id, NewComment("reply"));
        await MarkCommentsUnread(firstComment, reply);
        (await fixture.Api.CountUnreadComments(thread.Id)).Should().Be(2);

        await fixture.Api.DeleteCommentThread(thread.Id);

        (await fixture.Api.GetUnreadComments(thread.Id).ToArrayAsync()).Should().BeEmpty();
        (await fixture.Api.CountUnreadComments(thread.Id)).Should().Be(0);
        fixture.DbContext.UnreadComments.Where(c => c.CommentThreadId == thread.Id).Should().BeEmpty();
    }

    [Fact]
    public async Task ReadStatus_DeletedUnreadCommentsDoNotInflateUnreadResults()
    {
        var authorId = $"author-{Guid.NewGuid()}";
        await ClearUnreadComments();
        var (thread, firstComment) = await CreateThreadWithComment(authorId);
        await MarkCommentsUnread(firstComment);
        (await fixture.Api.CountUnreadComments(thread.Id)).Should().Be(1);

        await fixture.Api.DeleteUserComment(firstComment.Id);

        (await fixture.Api.GetUnreadComments(thread.Id).ToArrayAsync()).Should().BeEmpty();
        (await fixture.Api.CountUnreadComments(thread.Id)).Should().Be(0);
    }
}
