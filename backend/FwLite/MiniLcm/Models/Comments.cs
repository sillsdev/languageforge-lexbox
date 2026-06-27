using System.Text.Json.Serialization;

namespace MiniLcm.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ThreadStatus
{
    Open,
    Closed
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SubjectType
{
    Entry,
    Sense,
    ExampleSentence
}

public record CommentThread : IObjectWithId<CommentThread>
{
    public Guid Id { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid SubjectId { get; set; }
    public SubjectType SubjectType { get; set; }
    public ThreadStatus Status { get; set; } = ThreadStatus.Open;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Guid[] GetReferences()
    {
        return [];
    }

    public void RemoveReference(Guid id, DateTimeOffset time)
    {
    }

    public CommentThread Copy()
    {
        return this with { };
    }
}

public record UserComment : IObjectWithId<UserComment>
{
    public Guid Id { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid CommentThreadId { get; set; }
    public Guid? PreviousCommentId { get; set; }
    public required string Text { get; set; }
    public string? AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Guid[] GetReferences()
    {
        return [CommentThreadId];
    }

    public void RemoveReference(Guid id, DateTimeOffset time)
    {
        if (id == CommentThreadId)
            DeletedAt = time;
    }

    public UserComment Copy()
    {
        return this with { };
    }
}
