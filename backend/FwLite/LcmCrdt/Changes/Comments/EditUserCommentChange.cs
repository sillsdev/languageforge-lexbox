using System.Text.Json.Serialization;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Comments;

public class EditUserCommentChange : EditChange<UserComment>, ISelfNamedType<EditUserCommentChange>
{
    public EditUserCommentChange(Guid entityId, string text, DateTimeOffset updatedAt) : base(entityId)
    {
        Text = text;
        UpdatedAt = updatedAt;
    }

    [JsonConstructor]
    private EditUserCommentChange(Guid entityId, string text) : base(entityId)
    {
        Text = text;
    }

    public string Text { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public override ValueTask ApplyChange(UserComment entity, IChangeContext context)
    {
        entity.Text = Text;
        entity.UpdatedAt = UpdatedAt;
        return ValueTask.CompletedTask;
    }
}
