namespace Testing.Services;

public static class Constants
{
    public static readonly string BasePath = Path.Join(Path.GetTempPath(), "SR_Tests");
    public static readonly DirectoryInfo TemplateRepo = new(Path.Join(BasePath, "_template-repo_"));
    public static readonly SendReceiveAuth ManagerAuth = new("manager", TestingEnvironmentVariables.DefaultPassword);
    public static readonly SendReceiveAuth AdminAuth = new("admin", TestingEnvironmentVariables.DefaultPassword);
    public static readonly SendReceiveAuth InvalidPass = new("manager", "incorrect_pass");
    public static readonly SendReceiveAuth InvalidUser = new("invalid_user", TestingEnvironmentVariables.DefaultPassword);
    public static readonly SendReceiveAuth UnauthorizedUser = new("user", TestingEnvironmentVariables.DefaultPassword);
}
