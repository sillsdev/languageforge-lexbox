namespace Testing.Services;

public static class TestingEnvironmentVariables
{
    public static string ServerHostname = Environment.GetEnvironmentVariable("TEST_SERVER_HOSTNAME") ?? "localhost";
    public static readonly bool IsDev = ServerHostname.StartsWith("localhost");
    //scheme like https:// or http://
    public static string HttpScheme = (Environment.GetEnvironmentVariable("TEST_HTTP_SCHEME") ?? (IsDev ? "http" : "https")) + "://";
    /// <summary>
    /// url like http://localhost
    /// </summary>
    public static string ServerBaseUrl => $"{HttpScheme}{ServerHostname}";
    public static string StandardHgHostname = Environment.GetEnvironmentVariable("TEST_STANDARD_HG_HOSTNAME") ?? "hg.localhost";
    /// <summary>
    /// url like http://hg.localhost
    /// </summary>
    public static string StandardHgBaseUrl => $"{HttpScheme}{StandardHgHostname}";

    public static string ResumableHgHostname = Environment.GetEnvironmentVariable("TEST_RESUMABLE_HG_HOSTNAME") ?? "resumable.localhost";
    /// <summary>
    /// url like http://resumable.localhost
    /// </summary>
    public static string ResumableBaseUrl => $"{HttpScheme}{ResumableHgHostname}";

    public static string ProjectCode = Environment.GetEnvironmentVariable("TEST_PROJECT_CODE") ?? "sena-3";
    public static string DefaultPassword = Environment.GetEnvironmentVariable("TEST_DEFAULT_PASSWORD") ?? "pass";

    public static string GetTestHostName(this HgProtocol protocol)
    {
        return protocol switch
        {
            HgProtocol.Hgweb => StandardHgHostname,
            HgProtocol.Resumable => ResumableHgHostname,
            _ => throw new ArgumentOutOfRangeException(nameof(protocol), protocol, null)
        };
    }
}

public enum HgProtocol
{
    Hgweb,
    Resumable
}
