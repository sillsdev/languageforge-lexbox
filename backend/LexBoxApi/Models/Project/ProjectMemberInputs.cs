using LexCore.Entities;

namespace LexBoxApi.Models.Project;

public record AddProjectMemberInput(Guid ProjectId, string? UsernameOrEmail, Guid? UserId, ProjectRole Role, bool canInvite);

public record BulkAddProjectMembersInput(Guid ProjectId, string[] Usernames, ProjectRole Role, string PasswordHash, string Locale = User.DefaultLocalizationCode);

public record ChangeProjectMemberRoleInput(Guid ProjectId, Guid UserId, ProjectRole Role);
