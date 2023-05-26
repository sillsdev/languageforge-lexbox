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
    private readonly IOptions<HgConfig> _hgOptions;
    private readonly IOptions<SendReceiveConfig> _sendReceiveOptions;
    private readonly IProgress _progress;

    public SendReceiveService(IOptions<HgConfig> hgOptions, IOptions<SendReceiveConfig> sendReceiveOptions, IProgress progress)
    {
        _hgOptions = hgOptions;
        _sendReceiveOptions = sendReceiveOptions;
        _progress = progress;
    }

    public async Task<string> VerifyHgVersion()
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

    public async Task<string> CloneProject(string projectCode, string destDir)
    {
        var repoUrl = $"{_hgOptions.Value.HgWebUrl}/hg/{projectCode}";
        var flexBridgeOptions = new Dictionary<string, string>
        {
            { "fullPathToProject", destDir },
            { "fdoDataModelVersion", _sendReceiveOptions.Value.FdoDataModelVersion },
            { "languageDepotRepoName", "LexBox" },
            { "languageDepotRepoUri", repoUrl },
            { "deleteRepoIfNoSuchBranch", "false" },
        };
        LfMergeBridge.LfMergeBridge.Execute("Language_Forge_Clone", _progress, flexBridgeOptions, out string cloneResult);

        if (_progress is StringBuilderProgress)
        {
            cloneResult = cloneResult + ((StringBuilderProgress)_progress).Text;
            ((StringBuilderProgress)_progress).Clear();
        }
        return cloneResult;
    }

    public async Task<string> SendReceiveProject(string projectCode, string projectDir)
    {
        string fwdataFilename = System.IO.Path.Join(projectDir, $"{projectCode}.fwdata");
        var repoUrl = $"{_hgOptions.Value.HgWebUrl}/hg/{projectCode}";
        var flexBridgeOptions = new Dictionary<string, string>
        {
            { "fullPathToProject", projectDir },
            { "fwdataFilename", fwdataFilename },
            { "fdoDataModelVersion", _sendReceiveOptions.Value.FdoDataModelVersion },
            { "languageDepotRepoName", "LexBox" },
            { "languageDepotRepoUri", repoUrl },
            { "user", "LexBox" },
            { "commitMessage", "Testing" }
        };

        LfMergeBridge.LfMergeBridge.Execute("Language_Forge_Send_Receive", _progress, flexBridgeOptions, out string cloneResult);
        if (_progress is StringBuilderProgress)
        {
            cloneResult = cloneResult + ((StringBuilderProgress)_progress).Text;
            ((StringBuilderProgress)_progress).Clear();
        }
        return cloneResult;
    }
}
