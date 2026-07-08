using LcmCrdt.Utils;
using Microsoft.EntityFrameworkCore;

namespace LcmCrdt.Data;

public class LocalCommentReadStatusService(IDbContextFactory<LcmCrdtDbContext> dbContextFactory)
{
    public async Task<UserComment[]> GetUnreadComments(Guid? threadId = null)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var query = UnreadComments(dbContext);
        if (threadId is not null)
            query = query.Where(c => c.CommentThreadId == threadId);
        return await query.ToArrayAsync();
    }

    public async Task<UserComment[]> GetUnreadCommentsForSubject(SubjectType subjectType, Guid subjectId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var query =
            from unreadComment in UnreadComments(dbContext)
            join thread in dbContext.CommentThreads on unreadComment.CommentThreadId equals thread.Id
            where thread.SubjectType == subjectType && thread.SubjectId == subjectId
            select unreadComment;
        return await query.ToArrayAsync();
    }

    public async Task<int> CountUnreadComments(Guid? threadId = null)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var query = UnreadComments(dbContext);
        if (threadId is not null)
            query = query.Where(c => c.CommentThreadId == threadId);
        return await query.CountAsync();
    }

    public async Task MarkCommentRead(Guid commentId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.UnreadComments
            .Where(c => c.CommentId == commentId)
            .ExecuteDeleteAsync();
    }

    public async Task MarkThreadRead(Guid threadId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.UnreadComments
            .Where(c => c.CommentThreadId == threadId)
            .ExecuteDeleteAsync();
    }

    public async Task MarkAllRead()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.UnreadComments.ExecuteDeleteAsync();
    }

    public async Task RemoveUnreadComments(IEnumerable<Guid> commentIds)
    {
        var ids = commentIds.Distinct().ToArray();
        if (ids.Length == 0) return;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.UnreadComments
            .Where(c => ids.Contains(c.CommentId))
            .ExecuteDeleteAsync();
    }

    public async Task MarkCommentsUnread(IEnumerable<(Guid CommentId, Guid CommentThreadId)> comments)
    {
        var commentsToMark = comments
            .DistinctBy(c => c.CommentId)
            .ToArray();
        if (commentsToMark.Length == 0) return;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var commentIds = commentsToMark.Select(c => c.CommentId).ToArray();
        var alreadyUnread = await dbContext.UnreadComments
            .Where(c => commentIds.Contains(c.CommentId))
            .Select(c => c.CommentId)
            .ToArrayAsync();
        var alreadyUnreadSet = alreadyUnread.ToHashSet();
        var now = DateTimeOffset.UtcNow;
        var toInsert = commentsToMark
            .Where(comment => !alreadyUnreadSet.Contains(comment.CommentId))
            .Select(comment => new UnreadComment
            {
                CommentId = comment.CommentId,
                CommentThreadId = comment.CommentThreadId,
                MarkedUnreadAt = now
            })
            .ToArray();
        if (toInsert.Length == 0) return;

        dbContext.UnreadComments.AddRange(toInsert);
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException e) when (e.CausedByUniqueConstraintViolation())
        {
            // Concurrent callers can both pass the existence check; CommentId is the PK.
            dbContext.ChangeTracker.Clear();
            foreach (var unread in toInsert)
            {
                dbContext.UnreadComments.Add(unread);
                try
                {
                    await dbContext.SaveChangesAsync();
                }
                catch (DbUpdateException ex) when (ex.CausedByUniqueConstraintViolation())
                {
                    dbContext.ChangeTracker.Clear();
                }
            }
        }
    }

    private static IQueryable<UserComment> UnreadComments(LcmCrdtDbContext dbContext)
    {
        return
            from unread in dbContext.UnreadComments
            join comment in dbContext.UserComments on unread.CommentId equals comment.Id
            orderby comment.CreatedAt, comment.Id
            select comment;
    }
}
