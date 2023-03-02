using LexCore.Entities;

namespace LexBoxApi.Models.Project;

public record CreateProjectInput(string Name, string Description, string Code, ProjectType Type, RetentionPolicy RetentionPolicy);