namespace LexCore.Entities;

public class EntityBase
{
    public Guid Id { get; init; }
    public DateTimeOffset CreatedDate { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedDate { get; init; } = DateTimeOffset.UtcNow;
}