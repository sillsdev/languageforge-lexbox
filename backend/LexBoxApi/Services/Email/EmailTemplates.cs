using System.Text.Json.Serialization;
using LexBoxApi.Models.Project;

namespace LexBoxApi.Services.Email;

public record EmailTemplateBase(EmailTemplate Template);

[JsonConverter(typeof(JsonStringEnumConverter))]
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
