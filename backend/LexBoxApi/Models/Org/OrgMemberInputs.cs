using LexCore.Entities;
public record AddOrgMemberInput(Guid OrgId, string UsernameOrEmail, OrgRole OrgRole);
