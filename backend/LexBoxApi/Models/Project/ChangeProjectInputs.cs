namespace LexBoxApi.Models.Project;

public record ChangeProjectNameInput(Guid ProjectId, string Name);

public record ChangeProjectDescriptionInput(Guid ProjectId, string Description);

public record ChangeUserAccountDataInput(Guid UserId, string Email, string Name);

public record ChangeUserAccountByAdminInput(Guid UserId, string Email, string Name);

public record DeleteUserByAdminOrSelfInput(Guid UserId);

public record ChangeUserPasswordByAdminInput(Guid AdminId, Guid UserId, string Passwrod, string ConfirmPassword);
