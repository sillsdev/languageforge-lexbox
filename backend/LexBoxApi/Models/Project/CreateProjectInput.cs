using LexCore.Entities;

namespace LexBoxApi.Models.Project;

public record CreateProjectInput(Guid? Id, string Name, string? Description, string Code, ProjectType Type, RetentionPolicy RetentionPolicy);