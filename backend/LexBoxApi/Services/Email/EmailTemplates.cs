using System.Text.Json.Serialization;

namespace LexBoxApi.Services.Email;

public record EmailTemplateBase(EmailTemplate Template);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EmailTemplate
{
    ForgotPassword,
    VerifyEmailAddress,
    PasswordChanged,
}

public record ForgotPasswordEmail(string Name, string ResetUrl) : EmailTemplateBase(EmailTemplate.ForgotPassword);

public record VerifyAddressEmail(string Name, string VerifyUrl) : EmailTemplateBase(EmailTemplate.VerifyEmailAddress);

public record PasswordChangedEmail(string Name) : EmailTemplateBase(EmailTemplate.PasswordChanged);
