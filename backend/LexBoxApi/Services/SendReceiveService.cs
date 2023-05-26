using System.Diagnostics;
using System.Runtime.InteropServices;
using LexBoxApi.Config;
// using LexCore.Entities;
using LexData;
// using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LexBoxApi.Services;

public class SendReceiveService
{
    private readonly LexBoxDbContext _dbContext;
    private readonly IHgService _hgService;
    private readonly IOptions<HgConfig> _hgOptions;
    private readonly IOptions<SendReceiveConfig> _sendReceiveOptions;

    public SendReceiveService(LexBoxDbContext dbContext, IHgService hgService, IOptions<HgConfig> hgOptions, IOptions<SendReceiveConfig> sendReceiveOptions)
    {
        _dbContext = dbContext;
        _hgService = hgService;
        _hgOptions = hgOptions;
        _sendReceiveOptions = sendReceiveOptions;
    }

    public async Task<string> VerifyHgVersion()
    {
        string output = "";
        using (Process hg = new()) {
            string hgFilename = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "hg.exe" : "hg";
            hg.StartInfo.FileName = $"Mercurial/{hgFilename}";
            hg.StartInfo.Arguments = "version";
            hg.StartInfo.RedirectStandardOutput = true;

            hg.Start();
            output = hg.StandardOutput.ReadToEnd();
            await hg.WaitForExitAsync();
        }
        return output;
    }

    public async Task<bool> CloneProject(string projectCode, string destDir)
    {
        var repoUrl = $"{_hgOptions.Value.HgWebUrl}/hg/{projectCode}";
        var flexBridgeOptions = new Dictionary<string, string>
        {
            {"fullPathToProject", destDir },
            {"fdoDataModelVersion", _sendReceiveOptions.Value.FdoDataModelVersion },
            {"languageDepotRepoName", "LexBox" },
            {"languageDepotRepoUri", repoUrl },
        };

        // TODO: Actually call FLExBridge rather than simply simulating success here
        bool Success = true;
        return Success;
    }

    public async Task<bool> SendReceiveProject(string projectCode)
    {
        // Will be similar to CloneProject, but will use Language_Forge_Send_Receive action and will require two extra parameters, fwdataFilename and commitMessage
        throw new NotImplementedException();
        // return false;
    }
}
