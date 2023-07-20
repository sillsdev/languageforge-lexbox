namespace Testing.Services;

public static class TestingEnvironmentVariables
{
    public static string ServerHostname = Environment.GetEnvironmentVariable("TEST_SERVER_HOSTNAME") ?? "localhost";
    public static string StandardHgHostname = Environment.GetEnvironmentVariable("TEST_STANDARD_HG_HOSTNAME") ?? "hg.localhost";
    public static string ResumableHgHostname = Environment.GetEnvironmentVariable("TEST_RESUMABLE_HG_HOSTNAME") ?? "resumable.localhost";

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
