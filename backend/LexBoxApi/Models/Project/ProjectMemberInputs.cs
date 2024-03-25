using System.ComponentModel.DataAnnotations;
using LexCore.Entities;

namespace LexBoxApi.Models.Project;

public record AddProjectMemberInput(Guid ProjectId, string UsernameOrEmail, ProjectRole Role);

public record ChangeProjectMemberRoleInput(Guid ProjectId, Guid UserId, ProjectRole Role);
