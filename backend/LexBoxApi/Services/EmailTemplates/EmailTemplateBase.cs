using System.Text.Json.Serialization;

namespace LexBoxApi.Services.Email;

public record EmailTemplateBase(EmailTemplate Template);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EmailTemplate
{
    ForgotPassword
}