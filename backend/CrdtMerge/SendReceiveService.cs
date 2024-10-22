using Microsoft.Extensions.Options;

public class SendReceiveService(IOptions<HgConfig> hgConfig, IOptions<SRConfig> srConfig)
{
    public SendReceiveHelpers.LfMergeBridgeResult SendReceive(string projectCode, string? commitMessage = null)
    {
        var fwdataName = $"{projectCode}.fwdata";
        var fwdataPath = Path.Join(hgConfig.Value.RepoPath, fwdataName);
        return SendReceiveHelpers.SendReceive(
            fwdataPath: fwdataPath,
            baseUrl: hgConfig.Value.HgWebUrl,
            auth: new SendReceiveHelpers.SendReceiveAuth(srConfig.Value),
            fdoDataModelVersion: srConfig.Value.FdoDataModelVersion,
            projectCode: projectCode,
            commitMessage: commitMessage
        );
    }

    public SendReceiveHelpers.LfMergeBridgeResult Clone(string projectCode)
    {
        var fwdataName = $"{projectCode}.fwdata";
        var fwdataPath = Path.Join(hgConfig.Value.RepoPath, fwdataName);
        return SendReceiveHelpers.CloneProject(
            fwdataPath: fwdataPath,
            baseUrl: hgConfig.Value.HgWebUrl,
            auth: new SendReceiveHelpers.SendReceiveAuth(srConfig.Value),
            fdoDataModelVersion: srConfig.Value.FdoDataModelVersion,
            projectCode: projectCode
        );
    }
}
