using System.Diagnostics;
using System.Runtime.InteropServices;
using LexBoxApi.Config;
// using LexCore.Entities;
using LexData;
// using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SIL.Progress;

namespace Testing.Services;

public class SendReceiveService
{
    private readonly string _defaultBaseUrl;
    private const string fdoDataModelVersion = "7000072";
    private readonly IProgress _progress;

    public SendReceiveService(IProgress progress, string defaultBaseUrl = "http://localhost:8088/hg")
    {
        // _sendReceiveOptions = sendReceiveOptions;
        _defaultBaseUrl = defaultBaseUrl;
        _progress = progress;
    }

    public async Task<string> GetHgVersion()
    {
        string output = "";
        using (Process hg = new()) {
            string hgFilename = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "hg.exe" : "hg";
            hg.StartInfo.FileName = System.IO.Path.Join("Mercurial", hgFilename);
            hg.StartInfo.Arguments = "version";
            hg.StartInfo.RedirectStandardOutput = true;

            hg.Start();
            output = hg.StandardOutput.ReadToEnd();
            await hg.WaitForExitAsync();
        }
        return output;
    }

    public string CloneProject(string projectCode, string destDir, string? baseUrlOpt = null)
    {
        string baseUrl = baseUrlOpt ?? _defaultBaseUrl;
        string repoUrl = $"{baseUrl}/{projectCode}";
        var flexBridgeOptions = new Dictionary<string, string>
        {
            { "fullPathToProject", destDir },
            { "fdoDataModelVersion", fdoDataModelVersion },
            { "languageDepotRepoName", "LexBox" },
            { "languageDepotRepoUri", repoUrl },
            { "deleteRepoIfNoSuchBranch", "false" },
        };
        LfMergeBridge.LfMergeBridge.Execute("Language_Forge_Clone", _progress, flexBridgeOptions, out string cloneResult);

        if (_progress is StringBuilderProgress sbProgress)
        {
            cloneResult = cloneResult + sbProgress.Text;
            sbProgress.Clear();
        }
        return cloneResult;
    }

    public string SendReceiveProject(string projectCode, string projectDir, string? baseUrlOpt = null)
    {
        string fwdataFilename = System.IO.Path.Join(projectDir, $"{projectCode}.fwdata");
        string baseUrl = baseUrlOpt ?? _defaultBaseUrl;
        string repoUrl = $"{baseUrl}/{projectCode}";
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

        LfMergeBridge.LfMergeBridge.Execute("Language_Forge_Send_Receive", _progress, flexBridgeOptions, out string cloneResult);
        if (_progress is StringBuilderProgress sbProgress)
        {
            cloneResult = cloneResult + sbProgress.Text;
            sbProgress.Clear();
        }
        return cloneResult;
    }
}
