using System.Text.Json.Serialization;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Comments;

public class CreateUserCommentChange : CreateChange<UserComment>, ISelfNamedType<CreateUserCommentChange>
{
    public CreateUserCommentChange(UserComment comment) : base(comment.Id == Guid.Empty ? Guid.NewGuid() : comment.Id)
    {
        comment.Id = EntityId;
        CommentThreadId = comment.CommentThreadId;
        PreviousCommentId = comment.PreviousCommentId;
        Text = comment.Text;
        AuthorId = comment.AuthorId;
        AuthorName = comment.AuthorName;
        CreatedAt = comment.CreatedAt == default ? DateTimeOffset.UtcNow : comment.CreatedAt;
        UpdatedAt = comment.UpdatedAt == default ? CreatedAt : comment.UpdatedAt;
    }

    [JsonConstructor]
    private CreateUserCommentChange(Guid entityId, Guid commentThreadId, string text) : base(entityId)
    {
        CommentThreadId = commentThreadId;
        Text = text;
    }

    public Guid CommentThreadId { get; init; }
    public Guid? PreviousCommentId { get; set; }
    public string Text { get; set; }
    public string? AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public override async ValueTask<UserComment> NewEntity(Commit commit, IChangeContext context)
    {
        return new UserComment
        {
            Id = EntityId,
            CommentThreadId = CommentThreadId,
            PreviousCommentId = PreviousCommentId,
            Text = Text,
            AuthorId = AuthorId,
            AuthorName = AuthorName,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            DeletedAt = await context.IsObjectDeleted(CommentThreadId) ? commit.DateTime : null
        };
    }
}
