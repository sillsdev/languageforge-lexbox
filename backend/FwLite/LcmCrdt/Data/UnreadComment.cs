namespace LcmCrdt.Data;

public class UnreadComment
{
    public Guid CommentId { get; set; }
    public Guid CommentThreadId { get; set; }
    public DateTimeOffset MarkedUnreadAt { get; set; }
}
