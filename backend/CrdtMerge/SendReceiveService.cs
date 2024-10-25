using FwDataMiniLcmBridge;
using Microsoft.Extensions.Options;

namespace CrdtMerge;

public class SendReceiveService(IOptions<CrdtMergeConfig> config)
{
    public SendReceiveHelpers.LfMergeBridgeResult SendReceive(FwDataProject project, string? commitMessage = null)
    {
        return SendReceiveHelpers.SendReceive(
            project: project,
            baseUrl: config.Value.HgWebUrl,
            auth: new SendReceiveHelpers.SendReceiveAuth(config.Value),
            fdoDataModelVersion: config.Value.FdoDataModelVersion,
            commitMessage: commitMessage
        );
    }

    public SendReceiveHelpers.LfMergeBridgeResult Clone(FwDataProject project)
    {
        return SendReceiveHelpers.CloneProject(
            project: project,
            baseUrl: config.Value.HgWebUrl,
            auth: new SendReceiveHelpers.SendReceiveAuth(config.Value),
            fdoDataModelVersion: config.Value.FdoDataModelVersion
        );
    }
}
