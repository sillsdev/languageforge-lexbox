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

    public static LfMergeBridgeResult SendReceive(string fwdataPath, SendReceiveAuth? auth = null, string? projectCode = null, string? commitMessage = null)
    {
        // If projectCode not given, calculate it from the fwdataPath
        var fwdataInfo = new FileInfo(fwdataPath);
        if (fwdataInfo.Directory is null) throw new ArgumentException("Not allowed to Send/Receive root-level directories like C:\\", nameof(fwdataPath));
        projectCode ??= fwdataInfo.Name.EndsWith(".fwdata") ? fwdataInfo.Name[..^".fwdata".Length] : fwdataInfo.Name;

        var hostname = "localhost";
        // var hostname = "lexbox.org"; // TODO: Get from config instead of hardcoding lexbox.org

        var repoUrl = BuildSendReceiveUrl(hostname, projectCode, auth);

        var fdoDataModelVersion = "7000072"; // TODO: Determine
        var flexBridgeOptions = new Dictionary<string, string>
        {
            { "fullPathToProject", fwdataInfo.Directory.FullName },
            { "fwdataFilename", fwdataInfo.Name },
            { "fdoDataModelVersion", fdoDataModelVersion },
            { "languageDepotRepoName", "LexBox" },
            { "languageDepotRepoUri", repoUrl.AbsoluteUri },
            { "deleteRepoIfNoSuchBranch", "false" },
            { "user", "LexBox" },
        };
        if (commitMessage is not null) flexBridgeOptions["commitMessage"] = commitMessage;
        return CallLfMergeBridge("Language_Forge_Send_Receive", flexBridgeOptions);
    }

    public static LfMergeBridgeResult CloneProject(string fwdataPath, SendReceiveAuth? auth = null, string? projectCode = null)
    {
        // If projectCode not given, calculate it from the fwdataPath
        var fwdataInfo = new FileInfo(fwdataPath);
        if (fwdataInfo.Directory is null) throw new ArgumentException("Not allowed to Send/Receive root-level directories like C:\\", nameof(fwdataPath));
        projectCode ??= fwdataInfo.Name.EndsWith(".fwdata") ? fwdataInfo.Name[..^".fwdata".Length] : fwdataInfo.Name;

        var hostname = "localhost";
        // var hostname = "lexbox.org"; // TODO: Get from config instead of hardcoding lexbox.org

        var repoUrl = BuildSendReceiveUrl(hostname, projectCode, auth);

        var fdoDataModelVersion = "7000072"; // TODO: Determine
        var flexBridgeOptions = new Dictionary<string, string>
        {
            { "fullPathToProject", fwdataInfo.Directory.FullName },
            { "fdoDataModelVersion", fdoDataModelVersion },
            { "languageDepotRepoName", "LexBox" },
            { "languageDepotRepoUri", repoUrl.ToString() },
            { "deleteRepoIfNoSuchBranch", "false" },
        };
        return CallLfMergeBridge("Language_Forge_Clone", flexBridgeOptions);
    }
}
