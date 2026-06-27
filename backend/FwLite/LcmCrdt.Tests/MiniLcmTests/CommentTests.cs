namespace LcmCrdt.Tests.MiniLcmTests;

public class CommentTests(MiniLcmApiFixture fixture) : IClassFixture<MiniLcmApiFixture>
{
    private async Task SetCurrentUser(string userId, string? name = null)
    {
        var projectService = fixture.GetService<CurrentProjectService>();
        await projectService.UpdateLastUser(name ?? userId, userId);
        await projectService.UpdateUserRole(UserProjectRole.Editor);
    }

    private async Task MarkExistingCommentsRead(string userId)
    {
        await SetCurrentUser(userId);
        await fixture.Api.MarkAllCommentsRead();
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
        comments.Should().ContainSingle();
        comments[0].Text.Should().Be("hello");
        comments[0].AuthorId.Should().Be(authorId);
        comments[0].AuthorName.Should().Be("Author Name");
        comments[0].CreatedAt.Should().NotBe(default);
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
        await MarkExistingCommentsRead(firstAuthor);
        var (thread, _) = await CreateThreadWithComment(firstAuthor);
        await fixture.Api.SetCommentThreadStatus(thread.Id, ThreadStatus.Closed);

        await SetCurrentUser(secondAuthor);
        var reply = await fixture.Api.AddUserComment(thread.Id, NewComment("late reply"));

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
        await MarkExistingCommentsRead(readerId);
        var (thread1, firstComment1) = await CreateThreadWithComment(authorId);
        var reply1 = await fixture.Api.AddUserComment(thread1.Id, NewComment("reply 1"));
        var (thread2, _) = await CreateThreadWithComment(authorId);

        await SetCurrentUser(readerId);
        (await fixture.Api.CountUnreadComments()).Should().Be(3);

        await fixture.Api.MarkCommentRead(firstComment1.Id);
        (await fixture.Api.CountUnreadComments()).Should().Be(2);
        (await fixture.Api.CountUnreadComments(thread1.Id)).Should().Be(1);

        await fixture.Api.MarkCommentThreadRead(thread1.Id);
        (await fixture.Api.CountUnreadComments()).Should().Be(1);

        await fixture.Api.MarkAllCommentsRead();
        (await fixture.Api.CountUnreadComments()).Should().Be(0);
        (await fixture.Api.GetUnreadComments().ToArrayAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task ReadStatus_IsPerUser()
    {
        var reader1 = $"reader-{Guid.NewGuid()}";
        var reader2 = $"reader-{Guid.NewGuid()}";
        var authorId = $"author-{Guid.NewGuid()}";
        await MarkExistingCommentsRead(reader1);
        await MarkExistingCommentsRead(reader2);
        var (_, firstComment) = await CreateThreadWithComment(authorId);

        await SetCurrentUser(reader1);
        await fixture.Api.MarkCommentRead(firstComment.Id);

        await SetCurrentUser(reader2);
        (await fixture.Api.GetUnreadComments().ToArrayAsync()).Should().ContainSingle(c => c.Id == firstComment.Id);
    }

    [Fact]
    public async Task ReadStatus_RemainsSeenWhenCommentArrivesAfterSeenRow()
    {
        var readerId = $"reader-{Guid.NewGuid()}";
        var authorId = $"author-{Guid.NewGuid()}";
        var futureCommentId = Guid.NewGuid();
        await MarkExistingCommentsRead(readerId);
        var (thread, _) = await CreateThreadWithComment(authorId);

        await SetCurrentUser(readerId);
        await fixture.Api.MarkCommentRead(futureCommentId);

        await SetCurrentUser(authorId);
        await fixture.Api.AddUserComment(thread.Id, NewComment("arrived later", futureCommentId));

        await SetCurrentUser(readerId);
        (await fixture.Api.GetUnreadComments(thread.Id).ToArrayAsync()).Should().NotContain(c => c.Id == futureCommentId);
    }
}
