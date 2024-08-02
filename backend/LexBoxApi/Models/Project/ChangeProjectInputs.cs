using LexCore.Entities;

namespace LexBoxApi.Models.Project;

public record ChangeProjectNameInput(Guid ProjectId, string Name);

public record ChangeProjectDescriptionInput(Guid ProjectId, string Description);

public record SetProjectConfidentialityInput(Guid ProjectId, bool IsConfidential);

public record SetRetentionPolicyInput(Guid ProjectId, RetentionPolicy RetentionPolicy);

public record DeleteUserByAdminOrSelfInput(Guid UserId);

public record ResetProjectByAdminInput(string Code);
