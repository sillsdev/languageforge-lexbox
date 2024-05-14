namespace LexBoxApi.Models.Org;

public record ChangeOrgNameInput(Guid OrgId, string Name);
// TODO: Remove if we decide that orgs shouldn't have descriptions
// public record ChangeOrgDescriptionInput(Guid OrgId, string Description);
