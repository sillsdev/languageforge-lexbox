using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Testing.Browser.Base;

public partial class UnexpectedResponseException : SystemException
{
    public static string MaskUrl(string url)
    {
        return JwtRegex().Replace(url, "*****");
    }

    public UnexpectedResponseException(IResponse response)
    : this(response.StatusText, response.Status, response.Url)
    {
    }

    public UnexpectedResponseException(string statusText, int statusCode, string url)
    : base($"Unexpected response: {statusText} ({statusCode}). URL: {MaskUrl(url)}.") { }

    [GeneratedRegex("[A-Za-z0-9-_]{10,}\\.[A-Za-z0-9-_]{20,}\\.[A-Za-z0-9-_]{10,}")]
    private static partial Regex JwtRegex();
}
