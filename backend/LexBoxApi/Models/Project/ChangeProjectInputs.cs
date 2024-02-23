namespace LexBoxApi.Models.Project;

public record ChangeProjectNameInput(Guid ProjectId, string Name);

public record ChangeProjectDescriptionInput(Guid ProjectId, string Description);

public record ChangeDraftProjectNameInput(Guid ProjectId, string Name);

public record ChangeDraftProjectDescriptionInput(Guid ProjectId, string Description);

public record DeleteUserByAdminOrSelfInput(Guid UserId);

public record ResetProjectByAdminInput(string Code);
