using FwDataMiniLcmBridge;
using Microsoft.Extensions.Options;

namespace FwHeadless.Services;

public class SendReceiveService(IOptions<FwHeadlessConfig> config, SafeLoggingProgress progress) : ISendReceiveService
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

    public async Task<int> PendingCommitCount(FwDataProject project, string? projectCode)
    {
        return await SendReceiveHelpers.PendingMercurialCommits(
            project: project,
            projectCode: projectCode,
            baseUrl: config.Value.HgWebUrl,
            auth: new SendReceiveHelpers.SendReceiveAuth(config.Value),
            progress: progress
        );
    }

    public async Task CommitFile(string filePath, string commitMessage)
    {
        await SendReceiveHelpers.CommitFile(filePath, commitMessage, progress);
    }
}
