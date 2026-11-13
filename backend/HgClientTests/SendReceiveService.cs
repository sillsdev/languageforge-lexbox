using System.Diagnostics;
using System.Runtime.CompilerServices;
using Chorus;
using Nini.Ini;
using FluentAssertions;
using SIL.Progress;
using Testing.Services;
using Xunit.Abstractions;

namespace HgClientTests;

public record SendReceiveParams(string ProjectCode, string BaseUrl, string Dir) : ProjectPath(ProjectCode, Dir)
{
    public SendReceiveParams(HgProtocol protocol, ProjectPath project) : this(project.Code, protocol.GetTestHostName(), project.Dir) { }
}

public class SendReceiveService
{
    private readonly ITestOutputHelper _output;
    private const string fdoDataModelVersion = "7000072";
    private static readonly string FixFwDataDir = Path.Join(AppContext.BaseDirectory, "FixFwDataTool");
    private static readonly Lock LfMergeBridgeLock = new();

    public SendReceiveService(ITestOutputHelper output)
    {
        _output = output;
        FixupCaCerts();
    }

    private static bool _cacertsFixed = false;

    private static void FixupCaCerts()
    {
        if (_cacertsFixed) return;
        var caCertsPem = new []
        {
            "/etc/ssl/certs/ca-certificates.crt",
            Path.GetFullPath(Path.Join(MercurialLocation.PathToMercurialFolder, "cacert.pem")),
        } .FirstOrDefault(File.Exists);
        if (string.IsNullOrEmpty(caCertsPem)) throw new FileNotFoundException("unable to find cacert.pem");
        //this cacerts.rc file is what is used when doing a clone, all future actions on a repo use the hgrc file defined in the .hg folder
        var cacertsRcPath = Path.Join(MercurialLocation.PathToMercurialFolder, "default.d", "cacerts.rc");
        //the default.d folder doesn't exist in linux builds, so we modify the mercurial.ini file instead
        if (!File.Exists(cacertsRcPath))
        {
            cacertsRcPath = Path.Join(MercurialLocation.PathToMercurialFolder, "mercurial.ini");
        }

        var caCertsRc = new IniDocument(cacertsRcPath, IniFileType.MercurialStyle);
        caCertsRc.Sections.GetOrCreate("web").Set("cacerts", caCertsPem);
        caCertsRc.Save();
        _cacertsFixed = true;
    }

    private StringBuilderProgress NewProgress()
    {
        return new XunitStringBuilderProgress(_output)
        {
            ProgressIndicator = new NullProgressIndicator(), ShowVerbose = true
        };
    }

    public static async Task<string> GetHgVersion()
    {
        using Process hg = new();
        hg.StartInfo.FileName = MercurialLocation.PathToHgExecutable;
        if (!File.Exists(hg.StartInfo.FileName) && !File.Exists(hg.StartInfo.FileName + ".exe"))
        {
            throw new FileNotFoundException("unable to find HG executable", hg.StartInfo.FileName);
        }

        hg.StartInfo.Arguments = "version";
        hg.StartInfo.RedirectStandardOutput = true;

        hg.Start();
        var output = await hg.StandardOutput.ReadToEndAsync();
        await hg.WaitForExitAsync();

        return output;
    }

    public string RunCloneSendReceive(SendReceiveParams sendReceiveParams, SendReceiveAuth auth)
    {
        var projectDir = sendReceiveParams.Dir;
        var fwDataFile = sendReceiveParams.FwDataFile;

        // Clone
        var cloneResult = CloneProject(sendReceiveParams, auth);

        Directory.Exists(projectDir).Should().BeTrue($"Directory {projectDir} not found. Clone response: {cloneResult}");
        Directory.EnumerateFiles(projectDir).Should().Contain(fwDataFile);
        var fwDataFileInfo = new FileInfo(fwDataFile);
        fwDataFileInfo.Length.Should().BeGreaterThan(0);
        var fwDataFileOriginalLength = fwDataFileInfo.Length;

        // SendReceive
        var srResult = SendReceiveProject(sendReceiveParams, auth);

        srResult.Should().Contain("No changes from others");
        fwDataFileInfo.Refresh();
        fwDataFileInfo.Exists.Should().BeTrue();
        fwDataFileInfo.Length.Should().Be(fwDataFileOriginalLength);

        return $"Clone: {cloneResult}{Environment.NewLine}SendReceive: {srResult}";
    }

    public string CloneProject(SendReceiveParams sendReceiveParams, SendReceiveAuth auth, bool validateOutput = true)
    {
        var (projectCode, baseUrl, destDir) = sendReceiveParams;
        var (username, password) = auth;
        var progress = NewProgress();
        var repoUrl = new UriBuilder($"{TestingEnvironmentVariables.HttpScheme}{baseUrl}/{projectCode}");
        progress.WriteMessage($"Cloning {repoUrl} with user '{username}' ...");
        if (String.IsNullOrEmpty(username) && String.IsNullOrEmpty(password))
        {
            // No username or password supplied, so we explicitly do *not* save user settings
        }
        else
        {
            var chorusSettings = new Chorus.Model.ServerSettingsModel
            {
                Username = username,
                RememberPassword = false, // Necessary for tests to work on Linux
                Password = password
            };
            chorusSettings.SaveUserSettings();
            repoUrl.UserName = username;
            repoUrl.Password = password;
        }

        var flexBridgeOptions = new Dictionary<string, string>
        {
            { "fullPathToProject", destDir },
            { "fdoDataModelVersion", fdoDataModelVersion },
            { "languageDepotRepoName", "LexBox" },
            { "languageDepotRepoUri", repoUrl.ToString() },
            { "deleteRepoIfNoSuchBranch", "false" },
        };
        var cloneResult = RunLfMergeBridge("Language_Forge_Clone", progress, flexBridgeOptions);
        cloneResult += $"{Environment.NewLine}Progress out: {progress.Text}";

        if (validateOutput)
        {
            ValidateSendReceiveOutput(cloneResult);
        }

        return cloneResult;
    }

    public string SendReceiveProject(SendReceiveParams sendReceiveParams, SendReceiveAuth auth, string commitMessage = "Testing", bool validateOutput = true)
    {
        var (projectCode, baseUrl, destDir) = sendReceiveParams;
        var (username, password) = auth;
        var progress = NewProgress();
        var repoUrl = new UriBuilder($"{TestingEnvironmentVariables.HttpScheme}{baseUrl}/{projectCode}");
        progress.WriteMessage($"S/R for {repoUrl} with user '{username}' ...");
        if (String.IsNullOrEmpty(username) && String.IsNullOrEmpty(password))
        {
            // No username or password supplied, so we explicitly do *not* save user settings
        }
        else
        {
            var chorusSettings = new Chorus.Model.ServerSettingsModel
            {
                Username = username,
                RememberPassword = false, // Necessary for tests to work on Linux
                Password = password
            };
            chorusSettings.SaveUserSettings();
            repoUrl.UserName = username;
            repoUrl.Password = password;
        }

        string fwdataFilename = Path.Join(destDir, $"{projectCode}.fwdata");
        var flexBridgeOptions = new Dictionary<string, string>
        {
            { "fullPathToProject", destDir },
            { "fwdataFilename", fwdataFilename },
            { "fdoDataModelVersion", fdoDataModelVersion },
            { "languageDepotRepoName", "LexBox" },
            { "languageDepotRepoUri", repoUrl.ToString() },
            { "user", "LexBox" },
            { "commitMessage", commitMessage }
        };

        var cloneResult = RunLfMergeBridge("Language_Forge_Send_Receive", progress, flexBridgeOptions);
        cloneResult += "Progress out: " + progress.Text;

        if (validateOutput)
        {
            ValidateSendReceiveOutput(cloneResult);
        }

        return cloneResult;
    }

    /// <summary>
    /// LfMergeBridge runs FixFwData.exe from the current directory. FixFwData lives in its own folder because its
    /// dependencies clash with old LibChorus's.
    /// </summary>
    private static string RunLfMergeBridge(string action, IProgress progress, Dictionary<string, string> options)
    {
        lock (LfMergeBridgeLock)
        {
            var previousDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(FixFwDataDir);
            try
            {
                LfMergeBridge.LfMergeBridge.Execute(action, progress, options, out var result);
                return result;
            }
            finally
            {
                Directory.SetCurrentDirectory(previousDir);
            }
        }
    }

    public static void ValidateSendReceiveOutput(string srOutput)
    {
        srOutput.Should().NotContain("abort");
        srOutput.Should().NotContain("failure");
        srOutput.Should().NotContain("error");
    }

    public static SendReceiveParams GetParams(HgProtocol protocol,
        string? projectCode = null,
        [CallerMemberName] string projectName = "")
    {
        projectCode ??= TestingEnvironmentVariables.ProjectCode;
        return new SendReceiveParams(projectCode, protocol.GetTestHostName(), Utils.GetNewProjectDir(projectCode, projectName));
    }
}
