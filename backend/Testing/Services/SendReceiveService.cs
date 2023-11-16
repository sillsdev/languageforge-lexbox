using System.Diagnostics;
using System.Runtime.InteropServices;
using Chorus;
using Nini.Ini;
using SIL.Progress;
using Testing.Logging;
using Xunit.Abstractions;

namespace Testing.Services;

public record SendReceiveAuth(string Username, string Password);

public record SendReceiveParams(string ProjectCode, string BaseUrl, string DestDir)
{
    public string FwDataFile { get; } = Path.Join(DestDir, $"{ProjectCode}.fwdata");
}

public class SendReceiveService
{
    private readonly ITestOutputHelper _output;
    private const string fdoDataModelVersion = "7000072";

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

    public async Task<string> GetHgVersion()
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

    public string CloneProject(SendReceiveParams sendReceiveParams, SendReceiveAuth auth)
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
        string cloneResult;
        LfMergeBridge.LfMergeBridge.Execute("Language_Forge_Clone", progress, flexBridgeOptions, out cloneResult);
        cloneResult += $"{Environment.NewLine}Progress out: {progress.Text}";
        return cloneResult;
    }

    public string SendReceiveProject(SendReceiveParams sendReceiveParams, SendReceiveAuth auth, string commitMessage = "Testing")
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

        string cloneResult;

        LfMergeBridge.LfMergeBridge.Execute("Language_Forge_Send_Receive",
            progress,
            flexBridgeOptions,
            out cloneResult);

        cloneResult += "Progress out: " + progress.Text;

        return cloneResult;
    }
}
