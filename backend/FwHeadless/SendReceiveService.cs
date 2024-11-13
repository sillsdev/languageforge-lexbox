using FwDataMiniLcmBridge;
using FwHeadless.Services;
using Microsoft.Extensions.Options;

namespace FwHeadless;

public class SendReceiveService(IOptions<FwHeadlessConfig> config, SafeLoggingProgress progress)
{
    public async Task<SendReceiveHelpers.LfMergeBridgeResult> SendReceive(FwDataProject project, string? projectCode, string? commitMessage = null)
    {
        return await SendReceiveHelpers.SendReceive(
            project: project,
            projectCode: projectCode,
            baseUrl: config.Value.HgWebUrl,
            auth: new SendReceiveHelpers.SendReceiveAuth(config.Value),
            fdoDataModelVersion: config.Value.FdoDataModelVersion,
            commitMessage: commitMessage,
            progress: progress
        );
    }

    public async Task<SendReceiveHelpers.LfMergeBridgeResult> Clone(FwDataProject project, string? projectCode)
    {
        return await SendReceiveHelpers.CloneProject(
            project: project,
            projectCode: projectCode,
            baseUrl: config.Value.HgWebUrl,
            auth: new SendReceiveHelpers.SendReceiveAuth(config.Value),
            fdoDataModelVersion: config.Value.FdoDataModelVersion,
            progress: progress
        );
    }
}
