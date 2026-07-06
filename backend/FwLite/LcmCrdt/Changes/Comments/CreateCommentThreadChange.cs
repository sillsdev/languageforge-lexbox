using System.Text.Json.Serialization;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Comments;

public class CreateCommentThreadChange : CreateChange<CommentThread>, ISelfNamedType<CreateCommentThreadChange>
{
    public CreateCommentThreadChange(CommentThread thread) : base(thread.Id == Guid.Empty ? Guid.NewGuid() : thread.Id)
    {
        thread.Id = EntityId;
        SubjectId = thread.SubjectId;
        SubjectType = thread.SubjectType;
        Status = thread.Status;
        AuthorId = thread.AuthorId;
        AuthorName = thread.AuthorName;
        CreatedAt = thread.CreatedAt == default ? DateTimeOffset.UtcNow : thread.CreatedAt;
        UpdatedAt = thread.UpdatedAt == default ? CreatedAt : thread.UpdatedAt;
    }

    [JsonConstructor]
    private CreateCommentThreadChange(Guid entityId, Guid subjectId, SubjectType subjectType) : base(entityId)
    {
        SubjectId = subjectId;
        SubjectType = subjectType;
    }

    public Guid SubjectId { get; init; }
    public SubjectType SubjectType { get; init; }
    public ThreadStatus Status { get; set; }
    public string? AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public override ValueTask<CommentThread> NewEntity(Commit commit, IChangeContext context)
    {
        return ValueTask.FromResult(new CommentThread
        {
            Id = EntityId,
            SubjectId = SubjectId,
            SubjectType = SubjectType,
            Status = Status,
            AuthorId = AuthorId,
            AuthorName = AuthorName,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        });
    }
}
