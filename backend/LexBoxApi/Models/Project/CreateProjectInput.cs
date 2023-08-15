using System.ComponentModel.DataAnnotations;
using LexCore.Entities;

namespace LexBoxApi.Models.Project;

public record CreateProjectInput(
    Guid? Id,
    string Name,
    string? Description,
    [property: MinLength(4), RegularExpression(@"[a-z\-]+")]
    string Code,
    ProjectType Type,
    RetentionPolicy RetentionPolicy
);
