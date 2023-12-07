namespace LexCore.Entities;

public class EntityBase
{
    public Guid Id { get; init; }
    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedDate { get; set; } = DateTimeOffset.UtcNow;
    public void UpdateUpdatedDate() => UpdatedDate = DateTimeOffset.UtcNow;
}
