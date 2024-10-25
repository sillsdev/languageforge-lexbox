using Microsoft.Extensions.Options;

namespace CrdtMerge;

public class SendReceiveService(IOptions<CrdtMergeConfig> config)
{
    public SendReceiveHelpers.LfMergeBridgeResult SendReceive(string projectFolder, string projectCode, string? commitMessage = null)
    {
        var fwdataName = $"{projectCode}.fwdata";
        var fwdataPath = Path.Join(projectFolder, projectCode, fwdataName);
        return SendReceiveHelpers.SendReceive(
            fwdataPath: fwdataPath,
            baseUrl: config.Value.HgWebUrl,
            auth: new SendReceiveHelpers.SendReceiveAuth(config.Value),
            fdoDataModelVersion: config.Value.FdoDataModelVersion,
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
            baseUrl: config.Value.HgWebUrl,
            auth: new SendReceiveHelpers.SendReceiveAuth(config.Value),
            fdoDataModelVersion: config.Value.FdoDataModelVersion,
            projectCode: projectCode
        );
    }
}
