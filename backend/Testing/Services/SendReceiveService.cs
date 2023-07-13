using System.Diagnostics;
using System.Runtime.InteropServices;
using SIL.Progress;
using Testing.Logging;
using Xunit.Abstractions;

namespace Testing.Services;

public class SendReceiveService
{
    private readonly ITestOutputHelper _output;
    private readonly string _baseUrl;
    private const string fdoDataModelVersion = "7000072";

    public SendReceiveService(ITestOutputHelper output, string baseUrl = "http://localhost")
    {
        // _sendReceiveOptions = sendReceiveOptions;
        _output = output;
        _baseUrl = baseUrl;
    }

    private IProgress NewProgress()
    {
        return new XunitStringBuilderProgress(_output) { ProgressIndicator = new NullProgressIndicator() };
    }

    public async Task<string> GetHgVersion()
    {
        string output = "";
        using (Process hg = new()) {
            string hgFilename = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "hg.exe" : "hg";
            hg.StartInfo.FileName = Path.Join("Mercurial", hgFilename);
            if (!File.Exists(hg.StartInfo.FileName))
            {
                throw new FileNotFoundException("unable to find HG executable", hg.StartInfo.FileName);
            }
            hg.StartInfo.Arguments = "version";
            hg.StartInfo.RedirectStandardOutput = true;

            hg.Start();
            output = await hg.StandardOutput.ReadToEndAsync();
            await hg.WaitForExitAsync();
        }
        return output;
    }

    public string CloneProject(string projectCode, string destDir, string username, string password)
    {
        var progress = NewProgress();
        string repoUrl = $"{_baseUrl}/{projectCode}";
        if (String.IsNullOrEmpty(username) && String.IsNullOrEmpty(password)) {
            // No username or password supplied, so we explicitly do *not* save user settings
        } else {
            var chorusSettings = new Chorus.Model.ServerSettingsModel();
            chorusSettings.Username = username;
            chorusSettings.RememberPassword = false; // Necessary for tests to work on Linux
            chorusSettings.Password = password;
            chorusSettings.SaveUserSettings();
            repoUrl = repoUrl.Replace("http://",$"http://{username}:{password}@");
        }
        Console.WriteLine($"Cloning {repoUrl} with user {username} and password \"{password}\" ...");
        var flexBridgeOptions = new Dictionary<string, string>
        {
            { "fullPathToProject", destDir },
            { "fdoDataModelVersion", fdoDataModelVersion },
            { "languageDepotRepoName", "LexBox" },
            { "languageDepotRepoUri", repoUrl },
            { "deleteRepoIfNoSuchBranch", "false" },
        };
        string cloneResult = "";

        LfMergeBridge.LfMergeBridge.Execute("Language_Forge_Clone", progress, flexBridgeOptions, out cloneResult);
        if (progress is StringBuilderProgress sbProgress)
        {
            cloneResult = cloneResult + sbProgress.Text;
            sbProgress.Clear();
        }
        return cloneResult;
    }

    public string SendReceiveProject(string projectCode, string projectDir, string username, string password)
    {
        var progress = NewProgress();
        string repoUrl = $"{_baseUrl}/{projectCode}";
        if (String.IsNullOrEmpty(username) && String.IsNullOrEmpty(password)) {
            // No username or password supplied, so we explicitly do *not* save user settings
        } else {
            var chorusSettings = new Chorus.Model.ServerSettingsModel();
            chorusSettings.Username = username;
            chorusSettings.RememberPassword = false; // Necessary for tests to work on Linux
            chorusSettings.Password = password;
            chorusSettings.SaveUserSettings();
            repoUrl = repoUrl.Replace("http://",$"http://{username}:{password}@");
        }
        string fwdataFilename = Path.Join(projectDir, $"{projectCode}.fwdata");
        Console.WriteLine($"S/R for {repoUrl} with user {username} and password \"{password}\" ...");
        var flexBridgeOptions = new Dictionary<string, string>
        {
            { "fullPathToProject", projectDir },
            { "fwdataFilename", fwdataFilename },
            { "fdoDataModelVersion", fdoDataModelVersion },
            { "languageDepotRepoName", "LexBox" },
            { "languageDepotRepoUri", repoUrl },
            { "user", "LexBox" },
            { "commitMessage", "Testing" }
        };

        string cloneResult = "";

        LfMergeBridge.LfMergeBridge.Execute("Language_Forge_Send_Receive", progress, flexBridgeOptions, out cloneResult);
        if (progress is StringBuilderProgress sbProgress)
        {
            cloneResult = cloneResult + sbProgress.Text;
            sbProgress.Clear();
        }
        return cloneResult;
    }
}
