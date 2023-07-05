namespace Testing.Services;

public static class TestingEnvironmentVariables
{
    public static string ServerHostname = Environment.GetEnvironmentVariable("TEST_SERVER_HOSTNAME") ?? "localhost";
    public static string StandardHgHostname = Environment.GetEnvironmentVariable("TEST_STANDARD_HG_HOSTNAME") ?? "hg.localhost";
    public static string ResumableHgHostname = Environment.GetEnvironmentVariable("TEST_RESUMABLE_HG_HOSTNAME") ?? "resumable.localhost";
}
