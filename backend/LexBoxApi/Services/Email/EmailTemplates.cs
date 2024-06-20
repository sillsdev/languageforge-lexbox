using LexBoxApi.Models.Project;

namespace LexBoxApi.Services.Email;

public record EmailTemplateBase(EmailTemplate Template)
{
    /// <summary>
    /// passed to the renderer so that it can generate links to svelte pages. However a jwt should never be passed to Svelte
    /// if we need to use a jwt then it should hit the api directly and the api should redirect to the svelte page
    /// </summary>
    public string? BaseUrl { get; set; }
}

public enum EmailTemplate
{
    ForgotPassword,
    NewAdmin,
    VerifyEmailAddress,
    PasswordChanged,
    CreateAccountRequest,
    CreateProjectRequest,
    ApproveProjectRequest,
    UserAdded,
}

public record ForgotPasswordEmail(string Name, string ResetUrl, TimeSpan lifetime) : EmailTemplateBase(EmailTemplate.ForgotPassword);

public record NewAdminEmail(string Name, string AdminName, string AdminEmail) : EmailTemplateBase(EmailTemplate.NewAdmin);

public record VerifyAddressEmail(string Name, string VerifyUrl, bool newAddress, TimeSpan lifetime) : EmailTemplateBase(EmailTemplate.VerifyEmailAddress);

public record ProjectInviteEmail(string Email, string ProjectId, string ManagerName, string ProjectName, string VerifyUrl, TimeSpan lifetime) : EmailTemplateBase(EmailTemplate.CreateAccountRequest);

public record PasswordChangedEmail(string Name) : EmailTemplateBase(EmailTemplate.PasswordChanged);

public record CreateProjectRequestUser(string Name, string Email);
public record CreateProjectRequestEmail(string Name, CreateProjectRequestUser User, CreateProjectInput Project) : EmailTemplateBase(EmailTemplate.CreateProjectRequest);
public record ApproveProjectRequestEmail(string Name, CreateProjectRequestUser User, CreateProjectInput Project) : EmailTemplateBase(EmailTemplate.ApproveProjectRequest);
public record UserAddedEmail(string Name, string Email, string ProjectName, string ProjectCode) : EmailTemplateBase(EmailTemplate.UserAdded);
