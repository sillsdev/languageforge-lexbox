using LexCore.Entities;

namespace LexBoxApi.Models.Project;

public record AddProjectMemberInput(Guid ProjectId, string UserEmail, ProjectRole Role);