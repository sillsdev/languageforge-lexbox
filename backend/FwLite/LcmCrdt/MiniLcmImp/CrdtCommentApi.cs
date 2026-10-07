using FluentValidation;
using LcmCrdt.Changes.Comments;
using LcmCrdt.Data;
using LcmCrdt.Harmony;
using LinqToDB.Async;
using MiniLcm.Exceptions;
using SIL.Harmony.Changes;

namespace LcmCrdt.MiniLcmImp;

public class CrdtCommentApi(
    MiniLcmRepositoryFactory repoFactory,
    HarmonyChangeWriter harmonyChangeWriter,
    CurrentProjectService projectService,
    LocalCommentReadStatusService commentReadStatusService)
{
    private ProjectData ProjectData => projectService.ProjectData;

    public async IAsyncEnumerable<CommentThread> GetCommentThreads(SubjectType subjectType, Guid subjectId, bool includeComments = false)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var threads = repo.CommentThreads
            .Where(t => t.SubjectType == subjectType && t.SubjectId == subjectId);
        if (includeComments)
        {
            threads = Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.Include(threads, t => t.Comments!.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id));
        }

        threads = threads.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id);
        await foreach (var thread in threads.AsAsyncEnumerable())
        {
            yield return thread;
        }
    }

    public async Task<CommentThread?> GetCommentThread(Guid id)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.GetCommentThread(id);
    }

    public async IAsyncEnumerable<UserComment> GetUserComments(Guid threadId)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var comments = repo.UserComments
            .Where(c => c.CommentThreadId == threadId)
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id);
        await foreach (var comment in comments.AsAsyncEnumerable())
        {
            yield return comment;
        }
    }

    public async Task<UserComment?> GetUserComment(Guid id)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.GetUserComment(id);
    }

    public async IAsyncEnumerable<UserComment> GetUnreadComments(Guid? threadId = null)
    {
        foreach (var comment in await commentReadStatusService.GetUnreadComments(threadId))
        {
            yield return comment;
        }
    }

    public async IAsyncEnumerable<UserComment> GetUnreadCommentsForSubject(SubjectType subjectType, Guid subjectId)
    {
        foreach (var comment in await commentReadStatusService.GetUnreadCommentsForSubject(subjectType, subjectId))
        {
            yield return comment;
        }
    }

    public Task<int> CountUnreadComments(Guid? threadId = null)
    {
        return commentReadStatusService.CountUnreadComments(threadId);
    }

    public async Task<CommentThread> CreateCommentThread(CommentThread thread, UserComment firstComment)
    {
        if (thread.Id == Guid.Empty) thread.Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        StampCommentThreadAuthor(thread, now);
        firstComment.CommentThreadId = thread.Id;
        StampCommentAuthor(firstComment, now);

        await harmonyChangeWriter.AddChanges((IEnumerable<IChange>)[
            new CreateCommentThreadChange(thread),
            new CreateUserCommentChange(firstComment)
        ]);
        return await GetCommentThread(thread.Id) ?? throw NotFoundException.ForType<CommentThread>(thread.Id);
    }

    public async Task<UserComment> AddUserComment(Guid threadId, UserComment comment)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        _ = await repo.GetCommentThread(threadId) ?? throw NotFoundException.ForType<CommentThread>(threadId);
        comment.CommentThreadId = threadId;
        StampCommentAuthor(comment, DateTimeOffset.UtcNow);

        await harmonyChangeWriter.AddChange(new CreateUserCommentChange(comment));
        return await repo.GetUserComment(comment.Id) ?? throw NotFoundException.ForType<UserComment>(comment.Id);
    }

    public async Task<UserComment> EditUserComment(Guid commentId, string text)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var comment = await repo.GetUserComment(commentId) ?? throw NotFoundException.ForType<UserComment>(commentId);
        AssertCurrentUserCanChangeComment(comment);
        await harmonyChangeWriter.AddChange(new EditUserCommentChange(commentId, text, DateTimeOffset.UtcNow));
        return await repo.GetUserComment(commentId) ?? throw NotFoundException.ForType<UserComment>(commentId);
    }

    public async Task<CommentThread> SetCommentThreadStatus(Guid threadId, ThreadStatus status)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        _ = await repo.GetCommentThread(threadId) ?? throw NotFoundException.ForType<CommentThread>(threadId);
        await harmonyChangeWriter.AddChange(new SetCommentThreadStatusChange(threadId, status, DateTimeOffset.UtcNow));
        return await repo.GetCommentThread(threadId) ?? throw NotFoundException.ForType<CommentThread>(threadId);
    }

    public async Task DeleteUserComment(Guid commentId)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var comment = await repo.GetUserComment(commentId) ?? throw NotFoundException.ForType<UserComment>(commentId);
        AssertCurrentUserCanChangeComment(comment);
        await harmonyChangeWriter.AddChange(new DeleteChange<UserComment>(commentId));
        await commentReadStatusService.RemoveUnreadComments([commentId]);
    }

    public async Task DeleteCommentThread(Guid threadId)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        _ = await repo.GetCommentThread(threadId) ?? throw NotFoundException.ForType<CommentThread>(threadId);
        await harmonyChangeWriter.AddChange(new DeleteChange<CommentThread>(threadId));
        await commentReadStatusService.MarkThreadRead(threadId);
    }

    public Task MarkCommentRead(Guid commentId)
    {
        return commentReadStatusService.MarkCommentRead(commentId);
    }

    public Task MarkCommentThreadUnread(Guid threadId)
    {
        return commentReadStatusService.MarkThreadUnread(threadId);
    }

    public Task MarkCommentThreadRead(Guid threadId)
    {
        return commentReadStatusService.MarkThreadRead(threadId);
    }

    public Task MarkAllCommentsRead()
    {
        return commentReadStatusService.MarkAllRead();
    }

    private void StampCommentThreadAuthor(CommentThread thread, DateTimeOffset now)
    {
        if (thread.Id == Guid.Empty) thread.Id = Guid.NewGuid();
        thread.AuthorId = RequireCommentUserId();
        thread.AuthorName = ProjectData.LastUserName;
        thread.CreatedAt = thread.CreatedAt == default ? now : thread.CreatedAt;
        thread.UpdatedAt = thread.UpdatedAt == default ? thread.CreatedAt : thread.UpdatedAt;
    }

    private void StampCommentAuthor(UserComment comment, DateTimeOffset now)
    {
        if (comment.Id == Guid.Empty) comment.Id = Guid.NewGuid();
        comment.AuthorId = RequireCommentUserId();
        comment.AuthorName = ProjectData.LastUserName;
        comment.CreatedAt = comment.CreatedAt == default ? now : comment.CreatedAt;
        comment.UpdatedAt = comment.UpdatedAt == default ? comment.CreatedAt : comment.UpdatedAt;
    }

    private string RequireCommentUserId()
    {
        if (string.IsNullOrEmpty(ProjectData.LastUserId))
            throw new ValidationException("Cannot create or modify comments without a known user identity.");
        return ProjectData.LastUserId;
    }

    private void AssertCurrentUserCanChangeComment(UserComment comment)
    {
        if (comment.AuthorId == RequireCommentUserId()) return;
        throw new UnauthorizedAccessException("Only the comment author can edit or delete this comment.");
    }
}
