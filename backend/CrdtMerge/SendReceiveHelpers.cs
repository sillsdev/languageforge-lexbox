using SIL.Progress;

public static class SendReceiveHelpers
{
    public record ProjectPath(string Code, string Dir)
    {
        public string FwDataFile { get; } = Path.Join(Dir, $"{Code}.fwdata");
    }

    public record SendReceiveAuth(string Username, string Password)
    {
        public SendReceiveAuth(SRConfig srConfig) : this(srConfig.LexboxUsername, srConfig.LexboxPassword) { }
    };

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
        var baseUri = new Uri(baseUrl);
        var projectUri = new Uri(baseUri, projectCode);
        if (auth == null) return projectUri;
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
        var builder = new UriBuilder(projectUri);
        builder.UserName = auth.Username;
        builder.Password = auth.Password;
        return builder.Uri;
    }

    public static LfMergeBridgeResult SendReceive(string fwdataPath, string baseUrl = "http://localhost", SendReceiveAuth? auth = null, string fdoDataModelVersion = "7000072", string? projectCode = null, string? commitMessage = null)
    {
        // If projectCode not given, calculate it from the fwdataPath
        var fwdataInfo = new FileInfo(fwdataPath);
        if (fwdataInfo.Directory is null) throw new ArgumentException("Not allowed to Send/Receive root-level directories like C:\\", nameof(fwdataPath));
        projectCode ??= fwdataInfo.Name.EndsWith(".fwdata") ? fwdataInfo.Name[..^".fwdata".Length] : fwdataInfo.Name;

        var repoUrl = BuildSendReceiveUrl(baseUrl, projectCode, auth);

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
        // return CallLfMergeBridge("Language_Forge_Send_Receive", flexBridgeOptions);
        Console.WriteLine($"Would have called S/R with fw data path {fwdataPath}, project code {projectCode}, base URL {baseUrl}");
        return new LfMergeBridgeResult("", "");
    }

    public static LfMergeBridgeResult CloneProject(string fwdataPath, string baseUrl = "http://localhost", SendReceiveAuth? auth = null, string fdoDataModelVersion = "7000072", string? projectCode = null)
    {
        // If projectCode not given, calculate it from the fwdataPath
        var fwdataInfo = new FileInfo(fwdataPath);
        if (fwdataInfo.Directory is null) throw new ArgumentException("Not allowed to Send/Receive root-level directories like C:\\", nameof(fwdataPath));
        projectCode ??= fwdataInfo.Name.EndsWith(".fwdata") ? fwdataInfo.Name[..^".fwdata".Length] : fwdataInfo.Name;

        var repoUrl = BuildSendReceiveUrl(baseUrl, projectCode, auth);

        var flexBridgeOptions = new Dictionary<string, string>
        {
            { "fullPathToProject", fwdataInfo.Directory.FullName },
            { "fdoDataModelVersion", fdoDataModelVersion },
            { "languageDepotRepoName", "LexBox" },
            { "languageDepotRepoUri", repoUrl.ToString() },
            { "deleteRepoIfNoSuchBranch", "false" },
        };
        return CallLfMergeBridge("Language_Forge_Clone", flexBridgeOptions);
        // Console.WriteLine($"Would have called LF_Clone with fw data path {fwdataPath}, project code {projectCode}, base URL {baseUrl}");
        // return new LfMergeBridgeResult("", "");
    }
}
