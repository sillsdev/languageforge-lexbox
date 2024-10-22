using SIL.Progress;

static class SendReceiveServices
{
    public record ProjectPath(string Code, string Dir)
    {
        public string FwDataFile { get; } = Path.Join(Dir, $"{Code}.fwdata");
    }

    public record SendReceiveAuth(string Username, string Password);

    public record SendReceiveParams(string ProjectCode, string BaseUrl, string Dir) : ProjectPath(ProjectCode, Dir);

    public record LfMergeBridgeResult(string Output, string ProgressMessages);

    public static LfMergeBridgeResult CallLfMergeBridge(string method, IDictionary<string, string> flexBridgeOptions)
    {
        var progress = new StringBuilderProgress();
        LfMergeBridge.LfMergeBridge.Execute(method, progress, flexBridgeOptions.ToDictionary(), out var lfMergeBridgeOutputForClient);
        return new LfMergeBridgeResult(lfMergeBridgeOutputForClient, progress.ToString());
    }

    public static Uri BuildSendReceiveUrl(string baseUrl, string projectCode, SendReceiveAuth? auth)
    {
        var builder = new UriBuilder($"http://{baseUrl}/hg/{projectCode}");
        if (auth == null) return builder.Uri;
        // Stop Chorus from saving passwords, since we're not a GUI app (and it calls Windows-only APIs anyway)
        var chorusSettings = new Chorus.Model.ServerSettingsModel
        {
            RememberPassword = false,
            Username = auth.Username,
            Password = auth.Password
        };
        // Chorus relies too much on its global ServerSettingsModel.PasswordForSession variable
        chorusSettings.SaveUserSettings();
        // TODO: Consider a global S/R lock because of Chorus's PasswordForSession behavior
        builder.UserName = auth.Username;
        builder.Password = auth.Password;
        return builder.Uri;
    }

    public static LfMergeBridgeResult SendReceive(string fwdataPath, string commitMessage, SendReceiveAuth? auth = null, string? projectCode = null)
    {
        // If projectCode not given, calculate it from the fwdataPath
        var fwdataInfo = LocateFwDataFile(fwdataPath);
        projectCode ??= fwdataInfo.Name.EndsWith(".fwdata") ? fwdataInfo.Name[..^".fwdata".Length] : fwdataInfo.Name;

        var hostname = "localhost";
        // var hostname = "lexbox.org"; // TODO: Get from config instead of hardcoding lexbox.org

        var repoUrl = BuildSendReceiveUrl(hostname, projectCode, auth);

        var fdoDataModelVersion = "7000072"; // TODO: Determine
        var flexBridgeOptions = new Dictionary<string, string>
        {
            { "fullPathToProject", fwdataInfo.Directory?.FullName ?? "" },
            { "fwdataFilename", fwdataInfo.Name },
            { "fdoDataModelVersion", fdoDataModelVersion },
            { "languageDepotRepoName", "LexBox" },
            { "languageDepotRepoUri", repoUrl.AbsoluteUri },
            { "deleteRepoIfNoSuchBranch", "false" },
            { "user", "LexBox" },
            { "commitMessage", commitMessage }
        };
        return CallLfMergeBridge("Language_Forge_Send_Receive", flexBridgeOptions);
    }

    public static LfMergeBridgeResult CloneProject(string fwdataPath, SendReceiveAuth? auth = null, string? projectCode = null)
    {
        // If projectCode not given, calculate it from the fwdataPath
        var fwdataInfo = LocateFwDataFile(fwdataPath);
        projectCode ??= fwdataInfo.Name.EndsWith(".fwdata") ? fwdataInfo.Name[..^".fwdata".Length] : fwdataInfo.Name;

        var hostname = "localhost";
        // var hostname = "lexbox.org"; // TODO: Get from config instead of hardcoding lexbox.org

        var repoUrl = BuildSendReceiveUrl(hostname, projectCode, auth);

        var fdoDataModelVersion = "7000072"; // TODO: Determine
        var flexBridgeOptions = new Dictionary<string, string>
        {
            { "fullPathToProject", fwdataInfo.Directory?.FullName ?? "" },
            { "fdoDataModelVersion", fdoDataModelVersion },
            { "languageDepotRepoName", "LexBox" },
            { "languageDepotRepoUri", repoUrl.ToString() },
            { "deleteRepoIfNoSuchBranch", "false" },
        };
        return CallLfMergeBridge("Language_Forge_Clone", flexBridgeOptions);
    }

    // TODO: Simplify, we don't need all this logic since the API is going to know where the fwdata file is
    private static FileInfo? MaybeLocateFwDataFile(string input)
    {
        Console.WriteLine("Locating {0} ...", input);
        if (Directory.Exists(input)) {
            var dirInfo = new DirectoryInfo(input);
            var fname = dirInfo.Name + ".fwdata";
            Console.WriteLine("Directory exists, creating file {0}", fname);
            return new FileInfo(Path.Join(input, fname));
        } else if (File.Exists(input)) {
            Console.WriteLine("File exists, returning as is");
            return new FileInfo(input);
        } else if (File.Exists(input + ".fwdata")) {
            Console.WriteLine("File exists with .fwdata extension added");
            return new FileInfo(input + ".fwdata");
        } else {
            return null;
        }
    }

    private static FileInfo LocateFwDataFile(string input)
    {
        var result = MaybeLocateFwDataFile(input);
        if (result != null) return result;
        Console.WriteLine("File not found, returning {0} with maybe a .fwdata extension", input);
        if (input.EndsWith(".fwdata")) return new FileInfo(input);
        return new FileInfo(input + ".fwdata");
    }
}
