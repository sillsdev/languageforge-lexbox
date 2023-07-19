namespace LexBoxApi.Models.Project;

public record ChangeProjectNameInput(Guid ProjectId, string Name);

public record ChangeProjectDescriptionInput(Guid ProjectId, string Description);
