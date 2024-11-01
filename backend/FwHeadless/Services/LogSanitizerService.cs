using System.Text;
using Microsoft.Extensions.Options;

namespace FwHeadless;

public class LogSanitizerService
{
    private string Password { get; init; }
    private string Base64Password { get; init; }
    private string Base64UrlPassword { get; init; }
    private bool ShouldSanitize { get; init; }

    public LogSanitizerService(IOptions<FwHeadlessConfig> config)
    {
        Password = config.Value.LexboxPassword;
        Base64Password = Convert.ToBase64String(Encoding.UTF8.GetBytes(Password));
        Base64UrlPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes(Password)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        ShouldSanitize = Password != "pass";
    }

    public string SanitizeLogMessage(string original)
    {
        if (!ShouldSanitize) return original;
        return original
            .Replace(Password, "***")
            .Replace(Base64Password, "***")
            .Replace(Base64UrlPassword, "***");
    }
}
