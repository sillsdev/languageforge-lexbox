using Chorus.VcsDrivers.Mercurial;
using FwDataMiniLcmBridge;
using SIL.Progress;

namespace FwHeadless.Services;

public class SendReceiveException(string? message) : Exception(message);

public static class SendReceiveHelpers
{
    private const string HgUsername = "FieldWorks Lite";
    public record ProjectPath(string Code, string Dir)
    {
        public string FwDataFile { get; } = Path.Join(Dir, $"{Code}.fwdata");
    }

    public record SendReceiveAuth(string Username, string Password)
    {
        public SendReceiveAuth(FwHeadlessConfig config) : this(config.LexboxUsername, config.LexboxPassword) { }
    };

    public record LfMergeBridgeResult(string Output)
    {
        private readonly IProgress? _progress = null;
        public bool ErrorEncountered => _progress?.ErrorEncountered ?? false;

        /// <summary>
        /// This string in the output unambiguously indicates that the operation ultimately succeeded.
        /// </summary>
        private const string SUCCESS_INDICATOR = "Clone success";

        /// <summary>
        /// Indicates if the operation succeeded.
        ///
        /// Not all errors are fatal. E.g. This logged exception:
        /// https://github.com/sillsdev/chorus/blob/1ab24b9cd13563145e98a43e1551518c8f9cc303/src/LibChorus/VcsDrivers/Mercurial/HgRepository.cs#L2132
        /// ...results in ErrorEncountered being set to true:
        /// https://github.com/sillsdev/libpalaso/blob/a8fcda92501e349ac23db6dba179322eca7fe561/SIL.Core/Progress/MultiProgress.cs#L168
        /// ...even though the exception does not prevent success.
        /// </summary>
        public bool Success => !ErrorEncountered ||
            Output.Contains(SUCCESS_INDICATOR, StringComparison.Ordinal);

        public LfMergeBridgeResult(string output, IProgress progress) : this(output)
        {
            _progress = progress;
        }
    }

    private static async Task<LfMergeBridgeResult> CallLfMergeBridge(string method, IDictionary<string, string> flexBridgeOptions, IProgress? progress = null)
    {
        var sbProgress = new StringBuilderProgress();
        var combinedProgress = new CombiningProgress(sbProgress, progress);
        var lfMergeBridgeOutputForClient = await Task.Run(() =>
        {
            LfMergeBridge.LfMergeBridge.Execute(method, combinedProgress, flexBridgeOptions.ToDictionary(), out var output);
            return output;
        });
        return new LfMergeBridgeResult(lfMergeBridgeOutputForClient, sbProgress);
    }

    private static Uri BuildSendReceiveUrl(string baseUrl, string projectCode, SendReceiveAuth? auth, bool forChorus = true)
    {
        var baseUri = new Uri(baseUrl);
        var projectUri = new Uri(baseUri, projectCode);
        if (auth == null) return projectUri;
        if (forChorus)
        {
            // Stop Chorus from saving passwords, since we're not a GUI app (and it calls Windows-only APIs anyway)
            var chorusSettings = new Chorus.Model.ServerSettingsModel
            {
                RememberPassword = false,
                Username = auth.Username,
                Password = auth.Password
            };
            // Chorus relies too much on its global ServerSettingsModel.PasswordForSession variable
            chorusSettings.SaveUserSettings();
        }
        // TODO: Consider a global S/R lock because of Chorus's PasswordForSession behavior
        var builder = new UriBuilder(projectUri);
        builder.UserName = auth.Username;
        builder.Password = auth.Password;
        return builder.Uri;
    }

    public static async Task<int> PendingMercurialCommits(FwDataProject project, string? projectCode = null, string baseUrl = "http://localhost", SendReceiveAuth? auth = null, IProgress? progress = null)
    {
        using var activity = FwHeadlessActivitySource.Value.StartActivity();
        projectCode ??= project.Name;
        activity?.SetTag("app.project_code", projectCode);
        progress ??= new NullProgress();

        var fwdataInfo = new FileInfo(project.FilePath);
        if (!fwdataInfo.Exists) return -1; // If there's no local clone then `hg incoming` won't work, so -1 is used to mean "all the commits on the server will be pulled"
        if (fwdataInfo.Directory is null) throw new InvalidOperationException(
            $"Not allowed to Send/Receive root-level directories like C:\\, was '{project.FilePath}'");

        var repoUrl = BuildSendReceiveUrl(baseUrl, projectCode, auth, forChorus: false);
        var hgResult = await Task.Run(() => HgRunner.Run($"hg incoming -T \"node: {{node}}\\n\" {repoUrl}", fwdataInfo.Directory.FullName, 9999, progress));
        if (hgResult.ExitCode == 1)
        {
            // hg incoming exits with 1 if there were no changes
            return 0;
        }
        var output = hgResult.StandardOutput;
        if (string.IsNullOrEmpty(output))
        {
            return 0;
        }
        var lines = output.Split('\n').Where(line => line.StartsWith("node"));
        return lines.Count();
    }

    public static async Task CommitFile(string filePath, string commitMessage, IProgress? progress = null)
    {
        using var activity = FwHeadlessActivitySource.Value.StartActivity();
        activity?.SetTag("app.file_path", filePath);
        progress ??= new NullProgress();
        if (!File.Exists(filePath)) throw new FileNotFoundException($"File not found: {filePath}");
        var fileDir = Path.GetDirectoryName(filePath);
        ArgumentNullException.ThrowIfNull(fileDir);

        //we need to track the file, otherwise hg will not commit it
        await ExecuteHgSuccess($"hg add --config ui.username={EscapeShellArg(HgUsername)} {EscapeShellArg(filePath)}", fileDir, progress);
        await ExecuteHgSuccess($"hg commit --config ui.username={EscapeShellArg(HgUsername)} --message {EscapeShellArg(commitMessage)}", fileDir, progress);
    }

    private static string EscapeShellArg(string arg)
    {
        var quote = """
                    "
                    """;
        var escaped = arg.Replace(
            quote,
            """
            \"
            """);
        return $"{quote}{escaped}{quote}";
    }

    private static async Task ExecuteHgSuccess(string cmd, string folder, IProgress? progress = null)
    {
        var result = await Task.Run(() => HgRunner.Run(cmd, folder, 5, progress));
        //not using HgRepository because it does not fail when exit code is 1 because that means not added.
        if (result.ExitCode != 0)
        {
            throw new Exception($"""
                                 Failed to execute command {cmd}, with exit code {result.ExitCode},
                                 ======= output =======
                                 output: {result.StandardOutput}
                                 ======= error =======
                                 error: {result.StandardError}
                                 """);
        }
    }

    public static async Task<LfMergeBridgeResult> SendReceive(FwDataProject project, string? projectCode = null, string baseUrl = "http://localhost", SendReceiveAuth? auth = null, string fdoDataModelVersion = "7000072", string? commitMessage = null, IProgress? progress = null)
    {
        using var activity = FwHeadlessActivitySource.Value.StartActivity();
        projectCode ??= project.Name;
        activity?.SetTag("app.project_code", projectCode);
        var fwdataInfo = new FileInfo(project.FilePath);
        if (fwdataInfo.Directory is null) throw new InvalidOperationException(
            $"Not allowed to Send/Receive root-level directories like C:\\, was '{project.FilePath}'");

        var repoUrl = BuildSendReceiveUrl(baseUrl, projectCode, auth);

        var flexBridgeOptions = new Dictionary<string, string>
        {
            { "fullPathToProject", fwdataInfo.Directory.FullName },
            { "fwdataFilename", fwdataInfo.Name },
            { "fdoDataModelVersion", fdoDataModelVersion },
            { "languageDepotRepoName", "LexBox" },
            { "languageDepotRepoUri", repoUrl.AbsoluteUri },
            { "deleteRepoIfNoSuchBranch", "false" },
            { "user", HgUsername }, // Not necessary if username was set at clone time, but why not
        };
        if (commitMessage is not null) flexBridgeOptions["commitMessage"] = commitMessage;
        return await CallLfMergeBridge("Language_Forge_Send_Receive", flexBridgeOptions, progress);
    }

    public static async Task<LfMergeBridgeResult> CloneProject(FwDataProject project, string? projectCode = null, string baseUrl = "http://localhost", SendReceiveAuth? auth = null, string fdoDataModelVersion = "7000072", IProgress? progress = null)
    {
        using var activity = FwHeadlessActivitySource.Value.StartActivity();
        projectCode ??= project.Name;
        activity?.SetTag("app.project_code", projectCode);
        var fwdataInfo = new FileInfo(project.FilePath);
        if (fwdataInfo.Directory is null) throw new InvalidOperationException($"Not allowed to Send/Receive root-level directories like C:\\ '{project.FilePath}'");

        var repoUrl = BuildSendReceiveUrl(baseUrl, projectCode, auth);

        var flexBridgeOptions = new Dictionary<string, string>
        {
            { "fullPathToProject", fwdataInfo.Directory.FullName },
            { "fdoDataModelVersion", fdoDataModelVersion },
            { "languageDepotRepoName", "LexBox" },
            { "languageDepotRepoUri", repoUrl.ToString() },
            { "deleteRepoIfNoSuchBranch", "false" },
            { "user", HgUsername }
        };
        return await CallLfMergeBridge("Language_Forge_Clone", flexBridgeOptions, progress);
    }
}
