namespace LexBoxApi.Services.Email;

public record ForgotPasswordEmail(string Name, string ResetUrl): EmailTemplateBase(EmailTemplate.ForgotPassword);