namespace LexCore.Entities;

public class EntityBase
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedDate { get; init; }
    public required DateTimeOffset UpdatedDate { get; init; }
}