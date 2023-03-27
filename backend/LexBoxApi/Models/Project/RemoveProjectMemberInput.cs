namespace LexBoxApi.Models.Project;

public record RemoveProjectMemberInput(Guid ProjectId, Guid UserId);