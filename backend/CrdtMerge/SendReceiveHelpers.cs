using FwDataMiniLcmBridge;
using SIL.Progress;

namespace CrdtMerge;

public static class SendReceiveHelpers
{
    public record ProjectPath(string Code, string Dir)
    {
        public string FwDataFile { get; } = Path.Join(Dir, $"{Code}.fwdata");
    }

    public record SendReceiveAuth(string Username, string Password)
    {
        public SendReceiveAuth(CrdtMergeConfig config) : this(config.LexboxUsername, config.LexboxPassword) { }
    };

    public record LfMergeBridgeResult(string Output, string ProgressMessages);

    private static LfMergeBridgeResult CallLfMergeBridge(string method, IDictionary<string, string> flexBridgeOptions)
    {
        var progress = new StringBuilderProgress();
        LfMergeBridge.LfMergeBridge.Execute(method, progress, flexBridgeOptions.ToDictionary(), out var lfMergeBridgeOutputForClient);
        return new LfMergeBridgeResult(lfMergeBridgeOutputForClient, progress.ToString());
    }

    private static Uri BuildSendReceiveUrl(string baseUrl, string projectCode, SendReceiveAuth? auth)
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

    public static LfMergeBridgeResult SendReceive(FwDataProject project, string baseUrl = "http://localhost", SendReceiveAuth? auth = null, string fdoDataModelVersion = "7000072", string? commitMessage = null)
    {
        // If projectCode not given, calculate it from the fwdataPath
        var fwdataInfo = new FileInfo(project.FilePath);
        if (fwdataInfo.Directory is null) throw new InvalidOperationException(
            $"Not allowed to Send/Receive root-level directories like C:\\, was '{project.FilePath}'");

        var repoUrl = BuildSendReceiveUrl(baseUrl, project.Name, auth);

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

    public static LfMergeBridgeResult CloneProject(FwDataProject project, string baseUrl = "http://localhost", SendReceiveAuth? auth = null, string fdoDataModelVersion = "7000072")
    {
        // If projectCode not given, calculate it from the fwdataPath
        var fwdataInfo = new FileInfo(project.FilePath);
        if (fwdataInfo.Directory is null) throw new InvalidOperationException($"Not allowed to Send/Receive root-level directories like C:\\ '{project.FilePath}'");

        var repoUrl = BuildSendReceiveUrl(baseUrl, project.Name, auth);

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
