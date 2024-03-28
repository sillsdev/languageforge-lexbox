namespace LexBoxApi.GraphQL.CustomTypes;

public class MeDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string? Email { get; set; }
    public required string Locale { get; set; }
}
