using FwDataMiniLcmBridge;
using Microsoft.Extensions.Options;

namespace FwHeadless;

public class SendReceiveService(IOptions<FwHeadlessConfig> config)
{
    public SendReceiveHelpers.LfMergeBridgeResult SendReceive(FwDataProject project, string? projectCode, string? commitMessage = null)
    {
        return SendReceiveHelpers.SendReceive(
            project: project,
            projectCode: projectCode,
            baseUrl: config.Value.HgWebUrl,
            auth: new SendReceiveHelpers.SendReceiveAuth(config.Value),
            fdoDataModelVersion: config.Value.FdoDataModelVersion,
            commitMessage: commitMessage
        );
    }

    public SendReceiveHelpers.LfMergeBridgeResult Clone(FwDataProject project, string? projectCode)
    {
        return SendReceiveHelpers.CloneProject(
            project: project,
            projectCode: projectCode,
            baseUrl: config.Value.HgWebUrl,
            auth: new SendReceiveHelpers.SendReceiveAuth(config.Value),
            fdoDataModelVersion: config.Value.FdoDataModelVersion
        );
    }
}
