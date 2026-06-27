namespace LcmCrdt.Data;

public class SeenUserComment
{
    public required string UserId { get; set; }
    public Guid CommentId { get; set; }
    public DateTimeOffset SeenAt { get; set; }
}
