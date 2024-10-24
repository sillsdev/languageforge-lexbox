using Microsoft.Extensions.Options;

public class SendReceiveService(IOptions<SRConfig> srConfig)
{
    public SendReceiveHelpers.LfMergeBridgeResult SendReceive(string projectCode, string? commitMessage = null)
    {
        var fwdataName = $"{projectCode}.fwdata";
        var fwdataPath = Path.Join(srConfig.Value.ProjectStorageRoot, projectCode, fwdataName);
        return SendReceiveHelpers.SendReceive(
            fwdataPath: fwdataPath,
            baseUrl: srConfig.Value.HgWebUrl,
            auth: new SendReceiveHelpers.SendReceiveAuth(srConfig.Value),
            fdoDataModelVersion: srConfig.Value.FdoDataModelVersion,
            projectCode: projectCode,
            commitMessage: commitMessage
        );
    }

    public SendReceiveHelpers.LfMergeBridgeResult Clone(string projectCode)
    {
        var fwdataName = $"{projectCode}.fwdata";
        var fwdataPath = Path.Join(srConfig.Value.ProjectStorageRoot, projectCode, fwdataName);
        return SendReceiveHelpers.CloneProject(
            fwdataPath: fwdataPath,
            baseUrl: srConfig.Value.HgWebUrl,
            auth: new SendReceiveHelpers.SendReceiveAuth(srConfig.Value),
            fdoDataModelVersion: srConfig.Value.FdoDataModelVersion,
            projectCode: projectCode
        );
    }
}
