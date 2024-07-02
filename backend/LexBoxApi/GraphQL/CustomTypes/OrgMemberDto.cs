namespace LexBoxApi.GraphQL.CustomTypes;

public class OrgMemberDto
{
    public required Guid Id { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset UpdatedDate { get; set; }
    public DateTimeOffset LastActive { get; set; }
    public required string Name { get; set; }
    public required string? Email { get; set; }
    public required string? Username { get; set; }
    public required string LocalizationCode { get; set; }
    public required bool EmailVerified { get; set; }
    public required bool IsAdmin { get; set; }
    public required bool Locked { get; set; }
    public required bool CanCreateProjects { get; set; }
    public required OrgMemberDtoCreatedBy? CreatedBy { get; set; }
}

public class OrgMemberDtoCreatedBy
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}
