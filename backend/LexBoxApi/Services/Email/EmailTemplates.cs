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
    VerifyEmailAddress,
    PasswordChanged,
    CreateProjectRequest
}

public record ForgotPasswordEmail(string Name, string ResetUrl) : EmailTemplateBase(EmailTemplate.ForgotPassword);

public record VerifyAddressEmail(string Name, string VerifyUrl, bool newAddress) : EmailTemplateBase(EmailTemplate.VerifyEmailAddress);

public record PasswordChangedEmail(string Name) : EmailTemplateBase(EmailTemplate.PasswordChanged);

public record CreateProjectRequestUser(string Name, string Email);
public record CreateProjectRequestEmail(string Name, CreateProjectRequestUser User, CreateProjectInput Project): EmailTemplateBase(EmailTemplate.CreateProjectRequest);
