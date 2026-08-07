using System.Text.Json.Serialization;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Comments;

public class SetCommentThreadStatusChange : EditChange<CommentThread>, ISelfNamedType<SetCommentThreadStatusChange>
{
    public SetCommentThreadStatusChange(Guid entityId, ThreadStatus status, DateTimeOffset updatedAt) : base(entityId)
    {
        Status = status;
        UpdatedAt = updatedAt;
    }

    [JsonConstructor]
    private SetCommentThreadStatusChange(Guid entityId, ThreadStatus status) : base(entityId)
    {
        Status = status;
    }

    public ThreadStatus Status { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public override ValueTask ApplyChange(CommentThread entity, IChangeContext context)
    {
        entity.Status = Status;
        entity.UpdatedAt = UpdatedAt;
        return ValueTask.CompletedTask;
    }
}
