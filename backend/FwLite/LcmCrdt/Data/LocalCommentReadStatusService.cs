using Microsoft.EntityFrameworkCore;

namespace LcmCrdt.Data;

public class LocalCommentReadStatusService(IDbContextFactory<LcmCrdtDbContext> dbContextFactory)
{
    public async Task<UserComment[]> GetUnreadComments(string userId, Guid? threadId = null)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var query = UnreadComments(dbContext, userId);
        if (threadId is not null)
            query = query.Where(c => c.CommentThreadId == threadId);
        var comments = await query.ToArrayAsync();
        return [.. comments.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)];
    }

    public async Task<int> CountUnreadComments(string userId, Guid? threadId = null)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var query = UnreadComments(dbContext, userId);
        if (threadId is not null)
            query = query.Where(c => c.CommentThreadId == threadId);
        return await query.CountAsync();
    }

    public async Task MarkCommentRead(string userId, Guid commentId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await MarkCommentsRead(dbContext, userId, [commentId]);
    }

    public async Task MarkThreadRead(string userId, Guid threadId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var commentIds = await dbContext.UserComments
            .Where(c => c.CommentThreadId == threadId)
            .Select(c => c.Id)
            .ToArrayAsync();
        await MarkCommentsRead(dbContext, userId, commentIds);
    }

    public async Task MarkAllRead(string userId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var commentIds = await dbContext.UserComments
            .Select(c => c.Id)
            .ToArrayAsync();
        await MarkCommentsRead(dbContext, userId, commentIds);
    }

    private static IQueryable<UserComment> UnreadComments(LcmCrdtDbContext dbContext, string userId)
    {
        return dbContext.UserComments
            .Where(c => c.AuthorId != userId)
            .Where(c => !dbContext.SeenUserComments.Any(seen =>
                seen.UserId == userId &&
                seen.CommentId == c.Id));
    }

    private static async Task MarkCommentsRead(LcmCrdtDbContext dbContext, string userId, IReadOnlyCollection<Guid> commentIds)
    {
        if (commentIds.Count == 0) return;

        var alreadySeen = await dbContext.SeenUserComments
            .Where(s => s.UserId == userId && commentIds.Contains(s.CommentId))
            .Select(s => s.CommentId)
            .ToArrayAsync();
        var alreadySeenSet = alreadySeen.ToHashSet();
        var now = DateTimeOffset.UtcNow;
        dbContext.SeenUserComments.AddRange(commentIds
            .Where(commentId => !alreadySeenSet.Contains(commentId))
            .Select(commentId => new SeenUserComment
            {
                UserId = userId,
                CommentId = commentId,
                SeenAt = now
            }));
        await dbContext.SaveChangesAsync();
    }
}
