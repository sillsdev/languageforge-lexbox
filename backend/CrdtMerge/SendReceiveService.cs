using Microsoft.Extensions.Options;

public class SendReceiveService(IOptions<SRConfig> srConfig)
{
    public SendReceiveHelpers.LfMergeBridgeResult SendReceive(string projectFolder, string projectCode, string? commitMessage = null)
    {
        var fwdataName = $"{projectCode}.fwdata";
        var fwdataPath = Path.Join(projectFolder, projectCode, fwdataName);
        return SendReceiveHelpers.SendReceive(
            fwdataPath: fwdataPath,
            baseUrl: srConfig.Value.HgWebUrl,
            auth: new SendReceiveHelpers.SendReceiveAuth(srConfig.Value),
            fdoDataModelVersion: srConfig.Value.FdoDataModelVersion,
            projectCode: projectCode,
            commitMessage: commitMessage
        );
    }

    public SendReceiveHelpers.LfMergeBridgeResult Clone(string projectFolder, string projectCode)
    {
        var fwdataName = $"{projectCode}.fwdata";
        var fwdataPath = Path.Join(projectFolder, projectCode, fwdataName);
        return SendReceiveHelpers.CloneProject(
            fwdataPath: fwdataPath,
            baseUrl: srConfig.Value.HgWebUrl,
            auth: new SendReceiveHelpers.SendReceiveAuth(srConfig.Value),
            fdoDataModelVersion: srConfig.Value.FdoDataModelVersion,
            projectCode: projectCode
        );
    }
}
