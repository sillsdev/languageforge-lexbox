using System.ComponentModel.DataAnnotations;
using LexCore.Entities;

namespace LexBoxApi.Models.Project;

public record AddProjectMemberInput(Guid ProjectId, string UsernameOrEmail, ProjectRole Role);

public record BulkAddProjectMembersInput(Guid? ProjectId, string[] Usernames, ProjectRole Role, string PasswordHash);

public record ChangeProjectMemberRoleInput(Guid ProjectId, Guid UserId, ProjectRole Role);
